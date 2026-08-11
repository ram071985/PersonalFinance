using Microsoft.AspNetCore.Identity;
using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Auth;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Identity;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Api.Endpoints;

public static class AuthEndpoints
{
    public const string RefreshCookieName = "pf_refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap,
            IConfiguration config) =>
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Email"] = ["Email is already registered."]
                });
            }

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                DisplayName = request.Email.Split('@')[0]
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors
                    .GroupBy(e => e.Code)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                return Results.ValidationProblem(errors);
            }

            await financeBootstrap.InitializeForUserAsync(user.Id);
            var auth = await IssueTokensAsync(user, userManager, tokenService, http, config, rememberMe: false);
            return Results.Ok(auth);
        });

        group.MapPost("/login", async (
            LoginRequest request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap,
            ILoggerFactory loggerFactory,
            IConfiguration config) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            {
                logger.LogWarning("Failed login for {Email}", request.Email);
                return Results.Unauthorized();
            }

            // Dedupe/seed safely (IgnoreQueryFilters) — fixes historical duplicate categories
            await financeBootstrap.InitializeForUserAsync(user.Id);

            var auth = await IssueTokensAsync(user, userManager, tokenService, http, config, request.RememberMe);
            return Results.Ok(auth);
        });

        group.MapPost("/refresh", async (
            RefreshRequest? request,
            HttpContext http,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            ILoggerFactory loggerFactory,
            IConfiguration config) =>
        {
            var logger = loggerFactory.CreateLogger("Auth");

            var raw = request?.RefreshToken;
            if (string.IsNullOrWhiteSpace(raw))
                raw = http.Request.Cookies[RefreshCookieName];

            if (string.IsNullOrWhiteSpace(raw))
            {
                return Results.Problem(
                    title: "Invalid refresh token",
                    detail: "Sign in again.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var hash = TokenService.HashToken(raw);
            var user = userManager.Users.FirstOrDefault(u => u.RefreshTokenHash == hash);

            if (user is null
                || user.RefreshTokenExpiresAt is null
                || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("Refresh failed — invalid or expired token");
                DeleteRefreshCookie(http);
                return Results.Problem(
                    title: "Invalid refresh token",
                    detail: "Sign in again.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            // Preserve cookie lifetime style: if existing cookie has no max-age we can't know;
            // use remaining server expiry window — treat as remember if > 1 day left.
            var remember = user.RefreshTokenExpiresAt > DateTime.UtcNow.AddDays(1);
            var auth = await IssueTokensAsync(user, userManager, tokenService, http, config, remember);
            return Results.Ok(auth);
        });

        group.MapPost("/logout", async (
            HttpContext http,
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager) =>
        {
            // Invalidate server-side even if cookie-only (optional user id)
            if (currentUser.UserId is not null)
            {
                var user = await userManager.FindByIdAsync(currentUser.UserId);
                if (user is not null)
                {
                    user.RefreshTokenHash = null;
                    user.RefreshTokenExpiresAt = null;
                    await userManager.UpdateAsync(user);
                }
            }
            else
            {
                // Cookie-only logout: clear hash for matching cookie if present
                var raw = http.Request.Cookies[RefreshCookieName];
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    // TokenService needed — resolve from request services
                    var tokenService = http.RequestServices.GetRequiredService<TokenService>();
                    var hash = TokenService.HashToken(raw);
                    var user = userManager.Users.FirstOrDefault(u => u.RefreshTokenHash == hash);
                    if (user is not null)
                    {
                        user.RefreshTokenHash = null;
                        user.RefreshTokenExpiresAt = null;
                        await userManager.UpdateAsync(user);
                    }
                }
            }

            DeleteRefreshCookie(http);
            return Results.NoContent();
        });

        group.MapGet("/me", async (
            UserManager<ApplicationUser> userManager,
            ICurrentUserService currentUser) =>
        {
            if (!currentUser.IsAuthenticated || currentUser.UserId is null)
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(currentUser.UserId);
            if (user is null)
                return Results.Unauthorized();

            return Results.Ok(new UserInfoResponse(user.Id, user.Email!, user.DisplayName));
        }).RequireAuthorization();

        return app;
    }

    private static async Task<AuthResponse> IssueTokensAsync(
        ApplicationUser user,
        UserManager<ApplicationUser> userManager,
        TokenService tokenService,
        HttpContext http,
        IConfiguration config,
        bool rememberMe)
    {
        var (access, accessExpires) = tokenService.CreateAccessToken(user);
        var (rawRefresh, hash, refreshExpires) = tokenService.CreateRefreshToken();

        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = refreshExpires;
        await userManager.UpdateAsync(user);

        SetRefreshCookie(http, rawRefresh, refreshExpires, rememberMe, config);

        // Refresh token is httpOnly cookie only — not returned to JS
        return new AuthResponse(access, null, user.Email!, user.Id, accessExpires);
    }

    private static void SetRefreshCookie(
        HttpContext http,
        string rawRefresh,
        DateTime expiresUtc,
        bool rememberMe,
        IConfiguration config)
    {
        // Only mark Secure on HTTPS. TestServer (Testing) and local http must not set Secure
        // or the cookie is dropped and refresh-by-cookie fails.
        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/api/auth",
            IsEssential = true
        };

        if (rememberMe)
            options.Expires = expiresUtc;
        // else: session cookie (no Expires) — dies when browser closes

        http.Response.Cookies.Append(RefreshCookieName, rawRefresh, options);
    }

    private static void DeleteRefreshCookie(HttpContext http)
    {
        http.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = "/api/auth",
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
        // Also try non-secure delete for local http dev
        http.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            Path = "/api/auth",
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax
        });
    }
}

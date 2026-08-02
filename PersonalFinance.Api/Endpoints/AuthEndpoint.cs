using Microsoft.AspNetCore.Identity;
using PersonalFinance.Api.Filters;
using PersonalFinance.Core.Dtos.Auth;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Identity;
using PersonalFinance.Infrastructure.Services;

namespace PersonalFinance.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireRateLimiting("auth");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap) =>
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
                    .GroupBy(e => e.Code.Contains("Password") ? "Password" : "Email")
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Description).ToArray());
                return Results.ValidationProblem(errors);
            }

            await financeBootstrap.InitializeForUserAsync(user.Id);
            return Results.Ok(await IssueTokensAsync(user, userManager, tokenService));
        }).Validate<RegisterRequest>();

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PersonalFinance.Auth");

            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                logger.LogWarning("Login failed — unknown email {Email}", request.Email);
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var check = await signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (check.IsLockedOut)
            {
                logger.LogWarning("Login locked out for {Email}", request.Email);
                return Results.Problem(
                    title: "Account locked",
                    detail: "Too many failed attempts. Try again later.",
                    statusCode: StatusCodes.Status423Locked);
            }

            if (!check.Succeeded)
            {
                logger.LogWarning("Login failed — bad password for {Email}", request.Email);
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            await financeBootstrap.InitializeForUserAsync(user.Id);
            logger.LogInformation("Login succeeded for {Email} UserId={UserId}", request.Email, user.Id);
            return Results.Ok(await IssueTokensAsync(user, userManager, tokenService));
        }).Validate<LoginRequest>();

        group.MapPost("/refresh", async (
            RefreshRequest request,
            UserManager<ApplicationUser> userManager,
            TokenService tokenService,
            ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("PersonalFinance.Auth");
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
                return Results.Unauthorized();

            var hash = TokenService.HashToken(request.RefreshToken);
            var users = userManager.Users.Where(u => u.RefreshTokenHash == hash).Take(1);
            var user = users.FirstOrDefault();

            if (user is null
                || user.RefreshTokenExpiresAt is null
                || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            {
                logger.LogWarning("Refresh failed — invalid or expired token");
                return Results.Problem(
                    title: "Invalid refresh token",
                    detail: "Sign in again.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            return Results.Ok(await IssueTokensAsync(user, userManager, tokenService));
        });

        group.MapPost("/logout", async (
            ICurrentUserService currentUser,
            UserManager<ApplicationUser> userManager) =>
        {
            if (currentUser.UserId is null)
                return Results.Unauthorized();

            var user = await userManager.FindByIdAsync(currentUser.UserId);
            if (user is not null)
            {
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                await userManager.UpdateAsync(user);
            }

            return Results.NoContent();
        }).RequireAuthorization();

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
        TokenService tokenService)
    {
        var (access, accessExpires) = tokenService.CreateAccessToken(user);
        var (rawRefresh, hash, refreshExpires) = tokenService.CreateRefreshToken();

        user.RefreshTokenHash = hash;
        user.RefreshTokenExpiresAt = refreshExpires;
        await userManager.UpdateAsync(user);

        return new AuthResponse(access, rawRefresh, user.Email!, user.Id, accessExpires);
    }
}

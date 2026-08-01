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
        var group = app.MapGroup("/api/auth").WithTags("Auth");

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
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.Description).ToArray());

                return Results.ValidationProblem(errors);
            }

            await financeBootstrap.InitializeForUserAsync(user.Id);

            var (token, expires) = tokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Email!, user.Id, expires));
        }).Validate<RegisterRequest>();

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var check = await signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (check.IsLockedOut)
            {
                return Results.Problem(
                    title: "Account locked",
                    detail: "Too many failed attempts. Try again later.",
                    statusCode: StatusCodes.Status423Locked);
            }

            if (!check.Succeeded)
            {
                return Results.Problem(
                    title: "Invalid credentials",
                    detail: "Invalid email or password.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            await financeBootstrap.InitializeForUserAsync(user.Id);

            var (token, expires) = tokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Email!, user.Id, expires));
        }).Validate<LoginRequest>();

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
}

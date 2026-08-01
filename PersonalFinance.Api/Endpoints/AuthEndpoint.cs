using Microsoft.AspNetCore.Identity;
using PersonalFinance.Infrastructure.Services;
using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Auth;
using PersonalFinance.Core.Interfaces;
using PersonalFinance.Infrastructure.Identity;

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
                return Results.BadRequest(new { message = "Email is already registered." });

            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                EmailConfirmed = true,
                DisplayName = request.Email.Split('@')[0]
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                return Results.BadRequest(new
                {
                    message = string.Join(" ", result.Errors.Select(e => e.Description))
                });

            // Claim legacy rows (null UserId) + seed defaults if needed
            await financeBootstrap.InitializeForUserAsync(user.Id);

            var (token, expires) = tokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Email!, user.Id, expires));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            TokenService tokenService,
            UserFinanceBootstrap financeBootstrap) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return Results.Unauthorized();

            var check = await signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);

            if (!check.Succeeded)
                return Results.Unauthorized();

            // Safety net for DBs that still have orphan rows
            await financeBootstrap.InitializeForUserAsync(user.Id);

            var (token, expires) = tokenService.CreateToken(user);
            return Results.Ok(new AuthResponse(token, user.Email!, user.Id, expires));
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
}

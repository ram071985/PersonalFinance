using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User?.FindFirstValue("sub");

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue("email");

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;
}
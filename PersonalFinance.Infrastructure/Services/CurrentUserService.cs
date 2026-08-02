using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PersonalFinance.Core.Interfaces;

namespace PersonalFinance.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private static readonly AsyncLocal<string?> OverrideUserId = new();

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    /// <summary>
    /// Background jobs: run work as a specific user without an HTTP context.
    /// </summary>
    public static IDisposable Impersonate(string userId)
    {
        var previous = OverrideUserId.Value;
        OverrideUserId.Value = userId;
        return new Restore(previous);
    }

    private sealed class Restore : IDisposable
    {
        private readonly string? _previous;
        public Restore(string? previous) => _previous = previous;
        public void Dispose() => OverrideUserId.Value = _previous;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        OverrideUserId.Value
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
        ?? User?.FindFirstValue("sub");

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? User?.FindFirstValue("email");

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(OverrideUserId.Value)
        || (User?.Identity?.IsAuthenticated ?? false);
}
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PersonalFinance.Services;

public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthTokenStore _tokenStore;

    public ServerAuthenticationStateProvider(AuthTokenStore tokenStore) =>
        _tokenStore = tokenStore;

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_tokenStore.IsAuthenticated)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _tokenStore.UserId ?? string.Empty),
            new Claim(ClaimTypes.Email, _tokenStore.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, _tokenStore.Email ?? string.Empty)
        };

        // authenticationType must be non-empty or IsAuthenticated stays false
        var identity = new ClaimsIdentity(claims, authenticationType: "Bearer");
        var user = new ClaimsPrincipal(identity);
        return Task.FromResult(new AuthenticationState(user));
    }

    public void NotifyAuthChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
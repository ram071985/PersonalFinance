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
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, _tokenStore.UserId ?? string.Empty),
            new(ClaimTypes.Email, _tokenStore.Email ?? string.Empty),
            new(ClaimTypes.Name, _tokenStore.Email ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
    }

    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
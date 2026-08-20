using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace PersonalFinance.Web.Services;

public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthTokenStore _tokenStore;

    public ServerAuthenticationStateProvider(AuthTokenStore tokenStore) =>
        _tokenStore = tokenStore;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _tokenStore.EnsureRestoredAsync();

        if (!_tokenStore.IsAuthenticated)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _tokenStore.Email ?? "user"),
            new(ClaimTypes.Email, _tokenStore.Email ?? ""),
            new(ClaimTypes.NameIdentifier, _tokenStore.UserId ?? "")
        };

        return new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "jwt")));
    }

    public void NotifyAuthChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
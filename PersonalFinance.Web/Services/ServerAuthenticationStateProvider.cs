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
        // Retry restore — first call often happens before JS is ready
        for (var i = 0; i < 8; i++)
        {
            await _tokenStore.EnsureRestoredAsync();
            if (_tokenStore.IsAuthenticated)
                break;
            await Task.Delay(40 * (i + 1));
        }

        if (!_tokenStore.IsAuthenticated)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, _tokenStore.Email ?? "user"),
            new(ClaimTypes.Email, _tokenStore.Email ?? ""),
            new(ClaimTypes.NameIdentifier, _tokenStore.UserId ?? "")
        };

        var identity = new ClaimsIdentity(claims, authenticationType: "jwt");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
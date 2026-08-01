using System.Net.Http.Headers;

namespace PersonalFinance.Services;

public class AuthDelegatingHandler : DelegatingHandler
{
    private readonly AuthTokenStore _tokenStore;

    public AuthDelegatingHandler(AuthTokenStore tokenStore) =>
        _tokenStore = tokenStore;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (_tokenStore.IsAuthenticated &&
            !string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
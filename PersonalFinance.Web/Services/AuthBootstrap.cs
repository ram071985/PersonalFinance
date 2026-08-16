namespace PersonalFinance.Web.Services;

/// <summary>
/// Circuit-scoped gate so AuthorizeRouteView does not treat the user as anonymous
/// before sessionStorage has been read.
/// </summary>
public sealed class AuthBootstrap
{
    private readonly AuthTokenStore _tokens;
    private readonly AuthService _auth;
    private readonly ServerAuthenticationStateProvider _authState;
    private Task? _init;

    public AuthBootstrap(
        AuthTokenStore tokens,
        AuthService auth,
        ServerAuthenticationStateProvider authState)
    {
        _tokens = tokens;
        _auth = auth;
        _authState = authState;
    }

    public bool IsReady { get; private set; }

    public Task EnsureReadyAsync() => _init ??= InitAsync();

    private async Task InitAsync()
    {
        try
        {
            for (var i = 0; i < 10; i++)
            {
                await _tokens.EnsureRestoredAsync();
                if (_tokens.IsAuthenticated)
                    break;
                await Task.Delay(30 * (i + 1));
            }

            // Only try cookie refresh when we still have no access token
            if (!_tokens.IsAuthenticated)
                await _auth.TryRefreshAsync();

            if (_tokens.IsAuthenticated)
                _authState.NotifyAuthChanged();
        }
        finally
        {
            IsReady = true;
        }
    }
}
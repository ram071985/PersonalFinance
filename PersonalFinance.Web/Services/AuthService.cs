using System.Text.Json;
using Microsoft.JSInterop;

namespace PersonalFinance.Web.Services;

public class AuthService
{
    private readonly IJSRuntime _js;
    private readonly IConfiguration _config;
    private readonly AuthTokenStore _tokenStore;
    private readonly ServerAuthenticationStateProvider _authState;
    private readonly object _refreshLock = new();
    private Task<bool>? _refreshInFlight;

    public AuthService(
        IJSRuntime js,
        IConfiguration config,
        AuthTokenStore tokenStore,
        ServerAuthenticationStateProvider authState)
    {
        _js = js;
        _config = config;
        _tokenStore = tokenStore;
        _authState = authState;
    }

    private string ApiBase =>
        (_config["ApiBaseUrl"] ?? "https://localhost:7000/").TrimEnd('/');

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password, bool rememberMe = false)
    {
        try
        {
            var result = await _js.InvokeAsync<AuthFetchResult>(
                "pfAuth.login", ApiBase, email, password, rememberMe);

            if (result.Status == 401)
                return (false, "Invalid email or password.");

            if (!result.Ok)
                return (false, $"Login failed ({result.Status}): {result.Text}");

            if (!TryParseAuth(result.Text, out var token, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            // Refresh token lives in httpOnly cookie only
            _tokenStore.SetRememberMe(rememberMe);
            _tokenStore.Set(token, refreshToken: null, userEmail, userId, expires);
            await _tokenStore.PersistAsync();
            _authState.NotifyAuthChanged();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Login error: {ex.Message}");
        }
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
    {
        try
        {
            var result = await _js.InvokeAsync<AuthFetchResult>(
                "pfAuth.register", ApiBase, email, password);

            if (!result.Ok)
            {
                if (TryGetMessage(result.Text, out var msg))
                    return (false, msg);
                return (false, $"Registration failed ({result.Status}): {result.Text}");
            }

            if (!TryParseAuth(result.Text, out var token, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            _tokenStore.SetRememberMe(false);
            _tokenStore.Set(token, refreshToken: null, userEmail, userId, expires);
            await _tokenStore.PersistAsync();
            _authState.NotifyAuthChanged();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Register error: {ex.Message}");
        }
    }

    /// <summary>Uses httpOnly refresh cookie via browser fetch. Returns false if re-login required.</summary>
    public Task<bool> TryRefreshAsync()
    {
        lock (_refreshLock)
        {
            _refreshInFlight ??= RefreshCoreAsync();
            return _refreshInFlight;
        }
    }

    private async Task<bool> RefreshCoreAsync()
    {
        try
        {
            await _tokenStore.EnsureRestoredAsync();

            var result = await _js.InvokeAsync<AuthFetchResult>("pfAuth.refresh", ApiBase);
            // Only clear session on definitive auth rejection — not on CORS/network blips
            if (result.Status == 401 || result.Status == 403)
            {
                _tokenStore.Clear();
                await _tokenStore.ClearPersistedAsync();
                _authState.NotifyAuthChanged();
                return false;
            }

            if (!result.Ok)
                return false;

            if (!TryParseAuth(result.Text, out var token, out var email, out var userId, out var expires, out _))
                return false;

            _tokenStore.Set(token, refreshToken: null, email, userId, expires);
            await _tokenStore.PersistAsync();
            _authState.NotifyAuthChanged();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            lock (_refreshLock) _refreshInFlight = null;
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _js.InvokeAsync<AuthFetchResult>(
                "pfAuth.logout", ApiBase, _tokenStore.AccessToken);
        }
        catch { /* ignore */ }

        _tokenStore.Clear();
        await _tokenStore.ClearPersistedAsync();
        _authState.NotifyAuthChanged();
    }

    private static bool TryParseAuth(
        string body,
        out string token,
        out string email,
        out string userId,
        out DateTime expiresAt,
        out string error)
    {
        token = "";
        email = "";
        userId = "";
        expiresAt = DateTime.UtcNow.AddHours(1);
        error = "";

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            token = ReadString(root, "token", "Token", "accessToken", "access_token") ?? "";
            email = ReadString(root, "email", "Email") ?? "";
            userId = ReadString(root, "userId", "UserId", "id", "Id") ?? "";

            if (TryReadDate(root, out var exp))
                expiresAt = exp;

            if (string.IsNullOrWhiteSpace(token))
            {
                error = $"No token in response: {body}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = $"Could not parse auth response: {ex.Message}. Body: {body}";
            return false;
        }
    }

    private static bool TryGetMessage(string body, out string message)
    {
        message = "";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var m) ||
                doc.RootElement.TryGetProperty("Message", out m))
            {
                message = m.GetString() ?? "";
                return !string.IsNullOrWhiteSpace(message);
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static string? ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
        }
        return null;
    }

    private static bool TryReadDate(JsonElement root, out DateTime value)
    {
        value = default;
        foreach (var name in new[] { "expiresAt", "ExpiresAt", "expires", "Expires" })
        {
            if (!root.TryGetProperty(name, out var p)) continue;
            if (p.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(p.GetString(), out var parsed))
            {
                value = parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();
                return true;
            }
        }
        return false;
    }

    /// <summary>Shape returned from pfAuth.* JS helpers.</summary>
    private sealed class AuthFetchResult
    {
        public int Status { get; set; }
        public bool Ok { get; set; }
        public string Text { get; set; } = "";
    }
}

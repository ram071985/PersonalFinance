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
            if (string.IsNullOrWhiteSpace(ApiBase) || ApiBase.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            {
                // Still allow localhost for local dev; only warn shape
            }

            var result = await _js.InvokeAsync<AuthJsResult>(
                "pfAuth.login", ApiBase, email, password, rememberMe);

            if (result is null)
                return (false, "Login failed: no response from browser auth helper (pfAuth).");

            if (result.Status == 401)
                return (false, "Invalid email or password.");

            // Prefer flat interop fields; fall back to parsing response body (older JS / interop casing)
            var token = result.Token;
            var userEmail = result.Email;
            var userId = result.UserId;
            var expires = DateTime.UtcNow.AddHours(8);

            if (string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(result.Text))
            {
                if (TryParseAuth(result.Text, out var parsedToken, out var parsedEmail, out var parsedUserId, out var parsedExpires, out _))
                {
                    token = parsedToken;
                    userEmail = parsedEmail;
                    userId = parsedUserId;
                    expires = parsedExpires;
                }
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                    return (false, result.Error);
                if (result.Status == 0)
                    return (false, $"Login failed — check ApiBaseUrl ({ApiBase}) and browser console.");
                if (result.Status == 401)
                    return (false, "Invalid email or password.");
                return (false, $"Login failed ({result.Status}): no token in response.");
            }

            if (!string.IsNullOrWhiteSpace(result.ExpiresAt) &&
                DateTime.TryParse(result.ExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var expParsed))
            {
                expires = expParsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(expParsed, DateTimeKind.Utc)
                    : expParsed.ToUniversalTime();
            }

            _tokenStore.SetRememberMe(rememberMe);
            _tokenStore.Set(token, refreshToken: null, userEmail ?? email, userId ?? "", expires);
            await _tokenStore.PersistAsync();
            // Also force browser storage via JS helper (same payload shape as authInterop)
            try
            {
                await _js.InvokeAsync<bool>("pfAuth.saveAuth", new
                {
                    token,
                    refreshToken = (string?)null,
                    email = userEmail ?? email,
                    userId = userId ?? "",
                    expiresAt = expires.ToUniversalTime().ToString("o")
                }, rememberMe);
            }
            catch { /* non-fatal */ }

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
            var result = await _js.InvokeAsync<AuthJsResult>(
                "pfAuth.register", ApiBase, email, password);

            if (result is null)
                return (false, "Register failed: no response from browser auth helper.");

            if (!result.Ok || string.IsNullOrWhiteSpace(result.Token))
            {
                if (!string.IsNullOrWhiteSpace(result.Error))
                    return (false, result.Error);
                if (TryGetMessage(result.Text, out var msg))
                    return (false, msg);
                return (false, $"Registration failed ({result.Status}): {result.Text}");
            }

            var expires = DateTime.UtcNow.AddHours(8);
            if (!string.IsNullOrWhiteSpace(result.ExpiresAt) &&
                DateTime.TryParse(result.ExpiresAt, null, System.Globalization.DateTimeStyles.RoundtripKind, out var parsed))
            {
                expires = parsed.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                    : parsed.ToUniversalTime();
            }

            _tokenStore.SetRememberMe(false);
            _tokenStore.Set(result.Token, refreshToken: null, result.Email ?? email, result.UserId ?? "", expires);
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

            var result = await _js.InvokeAsync<AuthJsResult>("pfAuth.refresh", ApiBase);
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
            await _js.InvokeAsync<AuthJsResult>(
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

    /// <summary>Flat shape from pfAuth.login / register for reliable interop.</summary>
    private sealed class AuthJsResult
    {
        public int Status { get; set; }
        public bool Ok { get; set; }
        public string? Token { get; set; }
        public string? Email { get; set; }
        public string? UserId { get; set; }
        public string? ExpiresAt { get; set; }
        public string? Error { get; set; }
        public string Text { get; set; } = "";
    }
}


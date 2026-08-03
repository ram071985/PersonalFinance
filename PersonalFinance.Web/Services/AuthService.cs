using System.Text.Json;

namespace PersonalFinance.Web.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthTokenStore _tokenStore;
    private readonly ServerAuthenticationStateProvider _authState;
    private readonly object _refreshLock = new();
    private Task<bool>? _refreshInFlight;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        AuthTokenStore tokenStore,
        ServerAuthenticationStateProvider authState)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
        _authState = authState;
    }

    private HttpClient CreateClient() =>
        _httpClientFactory.CreateClient("AuthApi");

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        try
        {
            var http = CreateClient();
            var response = await http.PostAsJsonAsync("api/auth/login", new { email, password });
            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return (false, "Invalid email or password.");

            if (!response.IsSuccessStatusCode)
                return (false, $"Login failed ({(int)response.StatusCode}): {body}");

            if (!TryParseAuth(body, out var token, out var refresh, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            _tokenStore.Set(token, refresh, userEmail, userId, expires);
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
            var http = CreateClient();
            var response = await http.PostAsJsonAsync("api/auth/register", new { email, password });
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (TryGetMessage(body, out var msg))
                    return (false, msg);
                return (false, $"Registration failed ({(int)response.StatusCode}): {body}");
            }

            if (!TryParseAuth(body, out var token, out var refresh, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            _tokenStore.Set(token, refresh, userEmail, userId, expires);
            await _tokenStore.PersistAsync();
            _authState.NotifyAuthChanged();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Register error: {ex.Message}");
        }
    }

    /// <summary>Uses stored refresh token to get a new access token. Returns false if re-login required.</summary>
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
            if (string.IsNullOrWhiteSpace(_tokenStore.RefreshToken))
                return false;

            var http = CreateClient();
            var response = await http.PostAsJsonAsync(
                "api/auth/refresh",
                new { refreshToken = _tokenStore.RefreshToken });

            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                await _tokenStore.ClearPersistedAsync();
                _authState.NotifyAuthChanged();
                return false;
            }

            if (!TryParseAuth(body, out var token, out var refresh, out var email, out var userId, out var expires, out _))
            {
                await _tokenStore.ClearPersistedAsync();
                _authState.NotifyAuthChanged();
                return false;
            }

            _tokenStore.Set(token, refresh ?? _tokenStore.RefreshToken, email, userId, expires);
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
            if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
            {
                var http = CreateClient();
                using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
                req.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
                await http.SendAsync(req);
            }
        }
        catch { /* ignore */ }

        await _tokenStore.ClearPersistedAsync();
        _authState.NotifyAuthChanged();
    }

    private static bool TryParseAuth(
        string body,
        out string token,
        out string? refreshToken,
        out string email,
        out string userId,
        out DateTime expiresAt,
        out string? error)
    {
        token = "";
        refreshToken = null;
        email = "";
        userId = "";
        expiresAt = DateTime.UtcNow.AddHours(1);
        error = null;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            token = ReadString(root, "token", "Token", "accessToken", "access_token") ?? "";
            refreshToken = ReadString(root, "refreshToken", "RefreshToken", "refresh_token");
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
}

using System.Net.Http.Json;
using System.Text.Json;
using PersonalFinance.Services;

namespace PersonalFinance.Services;

public class AuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthTokenStore _tokenStore;
    private readonly ServerAuthenticationStateProvider _authState;

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
            var response = await http.PostAsJsonAsync(
                "api/auth/login",
                new { email, password });

            var body = await response.Content.ReadAsStringAsync();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                return (false, "Invalid email or password.");

            if (!response.IsSuccessStatusCode)
                return (false, $"Login failed ({(int)response.StatusCode}): {body}");

            if (!TryParseAuth(body, out var token, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            _tokenStore.Set(token, userEmail, userId, expires);
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
            var response = await http.PostAsJsonAsync(
                "api/auth/register",
                new { email, password });

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (TryGetMessage(body, out var msg))
                    return (false, msg);
                return (false, $"Registration failed ({(int)response.StatusCode}): {body}");
            }

            if (!TryParseAuth(body, out var token, out var userEmail, out var userId, out var expires, out var parseError))
                return (false, parseError);

            _tokenStore.Set(token, userEmail, userId, expires);
            await _tokenStore.PersistAsync();
            _authState.NotifyAuthChanged();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Register error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        await _tokenStore.ClearPersistedAsync();
        _authState.NotifyAuthChanged();
    }

    private static bool TryParseAuth(
        string body,
        out string token,
        out string email,
        out string userId,
        out DateTime expiresAt,
        out string? error)
    {
        token = "";
        email = "";
        userId = "";
        expiresAt = DateTime.UtcNow.AddHours(8);
        error = null;

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
}
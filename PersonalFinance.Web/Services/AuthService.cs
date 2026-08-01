using System.Net.Http.Json;
using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Auth;

namespace PersonalFinance.Services;

public class AuthService
{
    private readonly HttpClient _http;
    private readonly AuthTokenStore _tokenStore;
    private readonly ServerAuthenticationStateProvider _authState;

    public AuthService(
        HttpClient http,
        AuthTokenStore tokenStore,
        ServerAuthenticationStateProvider authState)
    {
        _http = http;
        _tokenStore = tokenStore;
        _authState = authState;
    }

    public async Task<(bool Success, string? Error)> LoginAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/login", new LoginRequest(email, password));

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            return (false, "Invalid email or password.");

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            return (false, string.IsNullOrWhiteSpace(body) ? "Login failed." : body);
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is null)
            return (false, "Invalid response from server.");

        _tokenStore.Set(auth.Token, auth.Email, auth.UserId, auth.ExpiresAt);
        _authState.NotifyAuthenticationStateChanged();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RegisterAsync(string email, string password)
    {
        var response = await _http.PostAsJsonAsync("api/auth/register", new RegisterRequest(email, password));

        if (!response.IsSuccessStatusCode)
        {
            try
            {
                var err = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
                if (err is not null && err.TryGetValue("message", out var msg))
                    return (false, msg);
            }
            catch { /* ignore */ }

            return (false, "Registration failed.");
        }

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        if (auth is null)
            return (false, "Invalid response from server.");

        _tokenStore.Set(auth.Token, auth.Email, auth.UserId, auth.ExpiresAt);
        _authState.NotifyAuthenticationStateChanged();
        return (true, null);
    }

    public void Logout()
    {
        _tokenStore.Clear();
        _authState.NotifyAuthenticationStateChanged();
    }
}

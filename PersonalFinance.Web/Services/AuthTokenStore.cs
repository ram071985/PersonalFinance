using System.Text.Json;
using Microsoft.JSInterop;

namespace PersonalFinance.Web.Services;

/// <summary>
/// Circuit-scoped JWT + refresh token store.
/// Remember me → localStorage (survives browser restart).
/// Otherwise → sessionStorage (tab session only).
/// </summary>
public class AuthTokenStore
{
    private const string StorageKey = "pf.auth.v1";
    private const string PreferenceKey = "pf.auth.remember";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _js;
    private bool _restoredFromBrowser;

    public AuthTokenStore(IJSRuntime js) => _js = js;

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? Email { get; private set; }
    public string? UserId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    /// <summary>When true, tokens are written to localStorage.</summary>
    public bool RememberMe { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow.AddMinutes(-1));

    public bool AccessTokenExpiringSoon =>
        ExpiresAt is not null && ExpiresAt <= DateTime.UtcNow.AddMinutes(2);

    public void Set(string token, string? refreshToken, string email, string userId, DateTime expiresAt)
    {
        AccessToken = token;
        RefreshToken = refreshToken;
        Email = email;
        UserId = userId;
        ExpiresAt = expiresAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
            : expiresAt.ToUniversalTime();
    }

    public void SetRememberMe(bool remember) => RememberMe = remember;

    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        Email = null;
        UserId = null;
        ExpiresAt = null;
    }

    public async Task EnsureRestoredAsync()
    {
        if (IsAuthenticated) return;
        if (_restoredFromBrowser) return;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                // Prefer dedicated helper (same key as login JS write)
                var loaded = await _js.InvokeAsync<AuthLoadResult?>("pfAuth.loadAuth");
                var json = loaded?.Json;
                var fromLocal = loaded?.Remember == true;
                if (string.IsNullOrWhiteSpace(json))
                {
                    json = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                    fromLocal = !string.IsNullOrWhiteSpace(json);
                    if (string.IsNullOrWhiteSpace(json))
                        json = await _js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
                }

                _restoredFromBrowser = true;
                if (string.IsNullOrWhiteSpace(json)) return;

                RememberMe = fromLocal;

                var stored = JsonSerializer.Deserialize<StoredAuth>(json, JsonOptions);
                if (stored is null || string.IsNullOrWhiteSpace(stored.Token)) return;

                // Missing/default expiresAt must NOT wipe a valid token (common after JS save)
                DateTime expires;
                if (stored.ExpiresAt == default || stored.ExpiresAt.Year < 2020)
                {
                    expires = DateTime.UtcNow.AddHours(8);
                }
                else
                {
                    expires = stored.ExpiresAt.Kind == DateTimeKind.Unspecified
                        ? DateTime.SpecifyKind(stored.ExpiresAt, DateTimeKind.Utc)
                        : stored.ExpiresAt.ToUniversalTime();
                }

                // Truly expired access token
                if (expires <= DateTime.UtcNow.AddMinutes(-1))
                {
                    if (!fromLocal)
                    {
                        // Keep storage — refresh may still work; do not clear here
                        Email = stored.Email;
                        UserId = stored.UserId;
                        return;
                    }
                    Email = stored.Email;
                    UserId = stored.UserId;
                    return;
                }

                Set(stored.Token, refreshToken: null, stored.Email, stored.UserId, expires);
                return;
            }
            catch (InvalidOperationException) { await Task.Delay(50 * (attempt + 1)); }
            catch (JSDisconnectedException) { return; }
            catch (JSException) { await Task.Delay(50 * (attempt + 1)); }
            catch
            {
                _restoredFromBrowser = true;
                Clear();
                return;
            }
        }
    }

    public async Task PersistAsync()
    {
        if (string.IsNullOrWhiteSpace(AccessToken) && string.IsNullOrWhiteSpace(RefreshToken))
        {
            await ClearPersistedAsync();
            return;
        }

        var payload = JsonSerializer.Serialize(new StoredAuth
        {
            Token = AccessToken ?? "",
            RefreshToken = RefreshToken,
            Email = Email ?? "",
            UserId = UserId ?? "",
            ExpiresAt = (ExpiresAt ?? DateTime.UtcNow.AddHours(1)).ToUniversalTime()
        }, JsonOptions);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var saved = await _js.InvokeAsync<bool>("pfAuth.saveAuth", payload, RememberMe);
                if (!saved)
                {
                    // Fallback direct storage APIs
                    await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
                    await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                    if (RememberMe)
                        await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, payload);
                    else
                        await _js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, payload);
                }

                _restoredFromBrowser = true;
                return;
            }
            catch (InvalidOperationException) { await Task.Delay(50 * (attempt + 1)); }
            catch (JSDisconnectedException) { return; }
            catch (JSException) { await Task.Delay(50 * (attempt + 1)); }
        }
    }

    public async Task ClearPersistedAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
                await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
                await _js.InvokeVoidAsync("sessionStorage.removeItem", PreferenceKey);
                await _js.InvokeVoidAsync("localStorage.removeItem", PreferenceKey);
                return;
            }
            catch (InvalidOperationException) { await Task.Delay(50 * (attempt + 1)); }
            catch (JSDisconnectedException) { return; }
            catch (JSException) { await Task.Delay(50 * (attempt + 1)); }
        }
    }

    private sealed class AuthLoadResult
    {
        public string? Json { get; set; }
        public bool Remember { get; set; }
    }

    private sealed class StoredAuth
    {
        public string Token { get; set; } = "";
        public string? RefreshToken { get; set; }
        public string Email { get; set; } = "";
        public string UserId { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}

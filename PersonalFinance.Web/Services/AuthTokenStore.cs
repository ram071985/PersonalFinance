using System.Text.Json;
using Microsoft.JSInterop;

namespace PersonalFinance.Services;

/// <summary>
/// Circuit-scoped JWT store. Persists across refresh via browser sessionStorage.
/// </summary>
public class AuthTokenStore
{
    private const string StorageKey = "pf.auth.v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly IJSRuntime _js;
    private bool _restoredFromBrowser;

    public AuthTokenStore(IJSRuntime js) => _js = js;

    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public string? UserId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrWhiteSpace(AccessToken)
        && (ExpiresAt is null || ExpiresAt > DateTime.UtcNow.AddMinutes(-1));

    public void Set(string token, string email, string userId, DateTime expiresAt)
    {
        AccessToken = token;
        Email = email;
        UserId = userId;
        ExpiresAt = expiresAt.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc)
            : expiresAt.ToUniversalTime();
    }

    public void Clear()
    {
        AccessToken = null;
        Email = null;
        UserId = null;
        ExpiresAt = null;
    }

    public async Task EnsureRestoredAsync()
    {
        if (IsAuthenticated)
            return;

        if (_restoredFromBrowser)
            return;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                var json = await _js.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
                _restoredFromBrowser = true;

                if (string.IsNullOrWhiteSpace(json))
                    return;

                var stored = JsonSerializer.Deserialize<StoredAuth>(json, JsonOptions);
                if (stored is null || string.IsNullOrWhiteSpace(stored.Token))
                    return;

                var expires = stored.ExpiresAt.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(stored.ExpiresAt, DateTimeKind.Utc)
                    : stored.ExpiresAt.ToUniversalTime();

                if (expires <= DateTime.UtcNow.AddMinutes(-1))
                {
                    await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
                    return;
                }

                Set(stored.Token, stored.Email, stored.UserId, expires);
                return;
            }
            catch (InvalidOperationException)
            {
                // JS runtime not ready yet
                await Task.Delay(50 * (attempt + 1));
            }
            catch (JSDisconnectedException)
            {
                return;
            }
            catch (JSException)
            {
                await Task.Delay(50 * (attempt + 1));
            }
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
        if (!IsAuthenticated)
        {
            await ClearPersistedAsync();
            return;
        }

        var payload = JsonSerializer.Serialize(new StoredAuth
        {
            Token = AccessToken!,
            Email = Email ?? "",
            UserId = UserId ?? "",
            ExpiresAt = (ExpiresAt ?? DateTime.UtcNow.AddHours(8)).ToUniversalTime()
        }, JsonOptions);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await _js.InvokeVoidAsync("sessionStorage.setItem", StorageKey, payload);
                _restoredFromBrowser = true;
                return;
            }
            catch (InvalidOperationException)
            {
                await Task.Delay(50 * (attempt + 1));
            }
            catch (JSException)
            {
                await Task.Delay(50 * (attempt + 1));
            }
            catch
            {
                return;
            }
        }
    }

    public async Task ClearPersistedAsync()
    {
        Clear();
        _restoredFromBrowser = true;
        try
        {
            await _js.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        }
        catch
        {
            // ignore
        }
    }

    private sealed class StoredAuth
    {
        public string Token { get; set; } = "";
        public string Email { get; set; } = "";
        public string UserId { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }
}

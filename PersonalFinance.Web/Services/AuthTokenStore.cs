namespace PersonalFinance.Services;

/// <summary>
/// Circuit-scoped token store. Token lives only on the server for Interactive Server.
/// </summary>
public class AuthTokenStore
{
    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public string? UserId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsAuthenticated =>
        !string.IsNullOrEmpty(AccessToken)
        && ExpiresAt is not null
        && ExpiresAt > DateTime.UtcNow;

    public void Set(string token, string email, string userId, DateTime expiresAt)
    {
        AccessToken = token;
        Email = email;
        UserId = userId;
        ExpiresAt = expiresAt;
    }

    public void Clear()
    {
        AccessToken = null;
        Email = null;
        UserId = null;
        ExpiresAt = null;
    }
}
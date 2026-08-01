namespace PersonalFinance.Services;

/// <summary>
/// Circuit-scoped store. JWT lives only on the server (Interactive Server).
/// </summary>
public class AuthTokenStore
{
    public string? AccessToken { get; private set; }
    public string? Email { get; private set; }
    public string? UserId { get; private set; }
    public DateTime? ExpiresAt { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken);

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
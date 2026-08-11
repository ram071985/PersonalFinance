using Microsoft.AspNetCore.Identity;

namespace PersonalFinance.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Opaque refresh token (stored hashed).</summary>
    public string? RefreshTokenHash { get; set; }
    
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>When true and PhoneNumber is set, budget alerts also go out as SMS.</summary>
    public bool SmsNotificationsEnabled { get; set; }
}
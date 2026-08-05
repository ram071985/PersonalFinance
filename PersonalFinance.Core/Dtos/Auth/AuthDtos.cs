using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PersonalFinance.Core.Dtos.Auth;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Password);

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    public string Password { get; set; } = "";

    /// <summary>When true, refresh cookie lasts RefreshTokenDays; otherwise session cookie.</summary>
    public bool RememberMe { get; set; }
}

public class RefreshRequest
{
    /// <summary>Optional when refresh token is supplied via httpOnly cookie.</summary>
    public string? RefreshToken { get; set; }
}

public record AuthResponse(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("refreshToken")] string? RefreshToken,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("expiresAt")] DateTime ExpiresAt);

public record UserInfoResponse(
    [property: JsonPropertyName("userId")] string UserId,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("displayName")] string? DisplayName);
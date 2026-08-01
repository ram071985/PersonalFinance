using System.ComponentModel.DataAnnotations;

namespace PersonalFinance.Core.Dtos.Auth;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(
    string Token,
    string Email,
    string UserId,
    DateTime ExpiresAt);

public record UserInfoResponse(
    string UserId,
    string Email,
    string? DisplayName);
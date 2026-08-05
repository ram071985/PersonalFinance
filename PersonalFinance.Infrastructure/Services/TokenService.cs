using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using PersonalFinance.Infrastructure.Identity;

namespace PersonalFinance.Infrastructure.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    /// <summary>Short-lived access token (default 1 hour).</summary>
    public (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user)
    {
        var hours = double.Parse(_config["Jwt:AccessTokenHours"] ?? _config["Jwt:ExpireHours"] ?? "1");
        var expires = DateTime.UtcNow.AddHours(hours);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.DisplayName ?? user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]
                                   ?? throw new InvalidOperationException("Jwt:Key is missing")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }

    /// <summary>Opaque refresh token (default 14 days). Returns raw token + hash to store.</summary>
    public (string RawToken, string Hash, DateTime ExpiresAt) CreateRefreshToken()
    {
        var days = double.Parse(_config["Jwt:RefreshTokenDays"] ?? "14");
        var expires = DateTime.UtcNow.AddDays(days);
        var bytes = RandomNumberGenerator.GetBytes(64);
        var raw = Convert.ToBase64String(bytes);
        var hash = HashToken(raw);
        return (raw, hash, expires);
    }

    public static string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    // Back-compat alias used by older call sites
    public (string Token, DateTime ExpiresAt) CreateToken(ApplicationUser user) =>
        CreateAccessToken(user);
}

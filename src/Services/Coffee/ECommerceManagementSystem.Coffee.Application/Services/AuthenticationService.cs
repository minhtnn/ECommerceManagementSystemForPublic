using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly JwtSetting _jwtSetting;
    private readonly ILogger _logger;
    private readonly byte[] _securityKeyBytes;

    public AuthenticationService(IOptions<JwtSetting> jwtSettings, ILogger logger)
    {
        _jwtSetting = jwtSettings.Value;
        _logger = logger;
        ValidateJwtSettings();
        _securityKeyBytes = Encoding.UTF8.GetBytes(_jwtSetting.SecurityKey);
    }

    public (DateTime accessTokenExpiry, DateTime refreshTokenExpiry) GetJwtExpireConfiguration()
    {
        var accessTokenExpiry = DateTime.UtcNow.AddMinutes(_jwtSetting.AccessTokenExpiry);
        var refreshTokenExpiry = DateTime.UtcNow.AddDays(_jwtSetting.RefreshTokenExpiry);
        return (accessTokenExpiry, refreshTokenExpiry);
    }

    public string GenerateAccessTokenAsync(Accounts account)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account));

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.AccountId, account.Id.ToString()),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        if (!string.IsNullOrWhiteSpace(account.Email))
            claims.Add(new Claim(CustomClaimTypes.Email, account.Email));
        if (!string.IsNullOrWhiteSpace(account.Username))
            claims.Add(new Claim(CustomClaimTypes.Username, account.Username));
        if (account.Role == ERole.BrandAdmin)
        {
            if (account.BrandAccounts != null && account.BrandAccounts?.Count > 0)
                claims.Add(new Claim(CustomClaimTypes.ReferenceId, account.BrandAccounts[0].BrandId.ToString()));
        }
        if (account.Role == ERole.EndCustomer)
        {
            if (account.CustomerAccounts != null && account.CustomerAccounts?.Count > 0)
                claims.Add(new Claim(CustomClaimTypes.ReferenceId, account.CustomerAccounts[0].CustomerId.ToString()));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(_jwtSetting.AccessTokenExpiry),
            Issuer = _jwtSetting.Issuer,
            Audience = _jwtSetting.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_securityKeyBytes),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshTokensAsync(Accounts account)
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
    #region Helper

    private void ValidateJwtSettings()
    {
        if (string.IsNullOrWhiteSpace(_jwtSetting.SecurityKey))
            throw new InvalidOperationException("JWT SecurityKey is not configured");

        if (_jwtSetting.SecurityKey.Length < 32)
            throw new InvalidOperationException("JWT SecurityKey must be at least 32 characters");

        if (string.IsNullOrWhiteSpace(_jwtSetting.Issuer))
            throw new InvalidOperationException("JWT Issuer is not configured");

        if (string.IsNullOrWhiteSpace(_jwtSetting.Audience))
            throw new InvalidOperationException("JWT Audience is not configured");
    }

    #endregion
}

public static class CustomClaimTypes
{
    public const string AccountId = "AccountId";
    public const string Role = "Role";
    public const string Email = "Email";
    public const string Username = "Username";
    public const string ReferenceId = "ReferenceId";
}
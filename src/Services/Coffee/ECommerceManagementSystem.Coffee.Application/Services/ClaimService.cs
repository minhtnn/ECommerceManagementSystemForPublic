using System.Security.Claims;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ECommerceManagementSystem.Coffee.Application.Services;

/// <summary>
/// Service implementation để truy cập claims từ HttpContext.User
/// </summary>
public class ClaimService : IClaimService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ClaimService> _logger;

    public ClaimService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ClaimService> logger)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Helper property để truy cập User từ HttpContext
    /// </summary>
    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    public Guid GetCurrentAccountId()
    {
        var accountIdClaim = User?.FindFirst(CustomClaimTypes.AccountId)?.Value;
        
        if (string.IsNullOrEmpty(accountIdClaim))
        {
            _logger.LogWarning("AccountId claim not found in token");
            throw new UnauthorizedAccessException("Account ID not found in authentication token");
        }

        if (!Guid.TryParse(accountIdClaim, out var accountId))
        {
            _logger.LogError("Invalid AccountId format in token: {AccountId}", accountIdClaim);
            throw new InvalidOperationException($"Invalid Account ID format: {accountIdClaim}");
        }

        return accountId;
    }

    public string GetCurrentUsername()
    {
        var username = User?.FindFirst(CustomClaimTypes.Username)?.Value;
        
        if (string.IsNullOrEmpty(username))
        {
            _logger.LogWarning("Username claim not found in token");
            throw new UnauthorizedAccessException("Username not found in authentication token");
        }

        return username;
    }

    public string? GetCurrentEmail()
    {
        return User?.FindFirst(CustomClaimTypes.Email)?.Value;
    }

    public string GetCurrentRole()
    {
        var role = User?.FindFirst(ClaimTypes.Role)?.Value;
        
        if (string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("Role claim not found in token");
            throw new UnauthorizedAccessException("Role not found in authentication token");
        }

        return role;
    }

    public ERole GetCurrentRoleEnum()
    {
        var roleString = GetCurrentRole();
        
        if (Enum.TryParse<ERole>(roleString, out var roleEnum))
        {
            return roleEnum;
        }

        _logger.LogError("Unable to parse role to ERoleName enum: {Role}", roleString);
        throw new InvalidOperationException($"Invalid role value: {roleString}");
    }

    public Guid GetCurrentReferenceId()
    {
        var accountReferenceIdClaim = User?.FindFirst(CustomClaimTypes.ReferenceId)?.Value;
        if (string.IsNullOrEmpty(accountReferenceIdClaim))
        {
            _logger.LogWarning("Account ReferenceId claim not found in token");
            throw new UnauthorizedAccessException("Account ReferenceId not found in authentication token");
        }

        if (!Guid.TryParse(accountReferenceIdClaim, out var accountReferenceId))
        {
            _logger.LogError("Invalid AccountId format in token: {AccountId}", accountReferenceIdClaim);
            throw new InvalidOperationException($"Invalid Account ID format: {accountReferenceIdClaim}");
        }

        return accountReferenceId;
    }

    public bool IsInRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
            return false;

        return User?.IsInRole(role) ?? false;
    }

    public bool HasClaim(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
            return false;

        return User?.HasClaim(c => c.Type == claimType) ?? false;
    }

    public string? GetClaimValue(string claimType)
    {
        if (string.IsNullOrWhiteSpace(claimType))
            return null;

        return User?.FindFirst(claimType)?.Value;
    }

    public IEnumerable<Claim> GetAllClaims()
    {
        return User?.Claims ?? Enumerable.Empty<Claim>();
    }

    public string? GetJwtId()
    {
        return User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    public DateTime? GetIssuedAt()
    {
        var iatClaim = User?.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
        
        if (string.IsNullOrEmpty(iatClaim))
            return null;

        if (long.TryParse(iatClaim, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
        }

        return null;
    }
}
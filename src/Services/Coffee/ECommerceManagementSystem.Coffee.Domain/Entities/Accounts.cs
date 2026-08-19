using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Accounts : EntityAuditBase<Guid>
{
    public required ERole Role { get; set; }
    public string? Username { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public EAccountStatus Status { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified {get; set;}
    public required string Email { get; set; }
    public bool? IsEmailVerified { get; set; }
    public DateTime? EmailVerifiedDate { get; set; }
    public string? EmailVerificationToken {get; set;}
    public DateTime? EmailVerificationTokenExpiry  { get; set; }
    public DateTime? LastOtpSentAt { get; set; }
    public string? GoogleId {get; set;}
    public EAuthProvider? AuthProvider { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public bool? PasswordResetTokenUsed { get; set; }
    public DateTime? PasswordResetTokenUsedAt { get; set; }
    public int? PasswordResetFailedAttempts { get; set; }
    public DateTime? PasswordResetLockedUntil { get; set; }
    public DateTime? PasswordLastChangedAt { get; set; }
    public int? PasswordChangedCount { get; set; }
    public virtual List<BrandAccounts> BrandAccounts { get; set; } = new List<BrandAccounts>();
    public virtual List<CustomerAccounts> CustomerAccounts { get; set; } = new List<CustomerAccounts>();
    public virtual List<RefreshTokens> RefreshTokens { get; set; } = new List<RefreshTokens>();
    public virtual List<PasswordResetAuditLogs> PasswordResetAuditLogs { get; set; } = new List<PasswordResetAuditLogs>();
}
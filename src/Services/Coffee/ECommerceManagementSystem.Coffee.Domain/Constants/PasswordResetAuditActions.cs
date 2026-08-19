namespace ECommerceManagementSystem.Coffee.Domain.Constants;

public static class PasswordResetAuditActions
{
    public const string TokenRequested = "TokenRequested";
    public const string TokenValidated = "TokenValidated";
    public const string TokenExpired = "TokenExpired";
    public const string PasswordReset = "PasswordReset";
    public const string FailedAttempt = "FailedAttempt";
    public const string AccountLocked = "AccountLocked";
    public const string AccountUnlocked = "AccountUnlocked";
}
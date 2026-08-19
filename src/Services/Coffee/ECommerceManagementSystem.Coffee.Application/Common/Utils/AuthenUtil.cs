using System.Security.Cryptography;

namespace ECommerceManagementSystem.Coffee.Application.Common.Utils;

public static class AuthenUtil
{
    private const int WorkFactor = 12;
    private const int OtpLength = 6;
    public const int OtpExpired = 15;

    public static (string passwordHash, string passwordSalt) HashPassword(string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        var passwordSalt = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        return (passwordHash, passwordSalt);
    }
    public static bool Verify(string password, string passwordHash, string passwordSalt)
    {
        var isPasswordValid = BCrypt.Net.BCrypt.Verify(password, passwordHash);
        return isPasswordValid;
    }
    public static string CreateOtpVerification()
    {

        var digits = "0123456789";
        var result = new char[OtpLength];

        using var rng = RandomNumberGenerator.Create();
        var buffer = new byte[OtpLength];

        rng.GetBytes(buffer);

        // chữ số đầu tiên: 1–9
        result[0] = digits[(buffer[0] % 9) + 1];

        for (int i = 1; i < OtpLength; i++)
        {
            result[i] = digits[buffer[i] % digits.Length];
        }

        return new string(result);
    }
    public static DateTime CreateOtpExpired()
    {
        return DateTime.UtcNow.AddMinutes(OtpExpired);
    }
    public static string GenerateSecretKey(int size = 32)
    {
        var bytes = new byte[size];

        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);

        return Convert.ToBase64String(bytes);
    }
    public static string GeneratePasswordResetToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        return token;
    }
    public static DateTime GeneratePasswordResetTokenExpiry(int minutesToExpire = 15)
    {
        return DateTime.UtcNow.AddMinutes(minutesToExpire);
    }
    public static string GetPartialToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return string.Empty;
        
        return token.Length > 8 ? token.Substring(0, 8) : token;
    }
    public static bool SecureCompare(string? a, string? b)
    {
        if (a == null || b == null)
            return a == b;
        
        if (a.Length != b.Length)
            return false;
        
        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        
        return result == 0;
    }
}
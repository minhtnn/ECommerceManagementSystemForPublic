namespace ECommerceManagementSystem.Coffee.Domain.Models.Settings;

public class JwtSetting
{
    public string SecurityKey { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int AccessTokenExpiry { get; set; }
    public int RefreshTokenExpiry { get; set; }
}
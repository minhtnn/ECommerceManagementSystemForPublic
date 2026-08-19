using System.Text.Json.Serialization;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Authentication;

public class LoginResponse
{
    // public string? FullName {get; set;}
    public string? Username { get; set; }
    // public string? PhoneNumber { get; set; }
    public ERole? Role { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    // public DateTime? RefreshTokenExpiry { get; set; }
    public bool ShouldUpdateRefreshToken { get; set; } = false;
    [JsonIgnore]
    public CookieInfo CookieInfo { get; set; }
}

public class CookieInfo
{
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime? Expiry { get; set; }
    public string Domain { get; set; } = string.Empty;
}
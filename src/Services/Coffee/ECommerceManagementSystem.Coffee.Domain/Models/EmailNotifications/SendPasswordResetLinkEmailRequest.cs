using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;

public class SendPasswordResetLinkEmailRequest
{
    public string BrandLogoBase64 { get; set; }
    public string? BrandName { get; set; }
    public string? CustomerName { get; set; }
    public required string ToEmail { get; set; }
    public required string ResetUrl { get; set; }
    public DateTime ExpiryTime { get; set; }
    public string? TimeMeasureUnit { get; set; }
}
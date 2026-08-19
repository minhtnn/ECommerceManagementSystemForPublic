using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;

public class SendOtpEmailRequest
{
    public string BrandLogoBase64 { get; set; }
    public string? BrandName { get; set; }
    public string? CustomerName { get; set; }
    public string? FromEmail { get; set; }
    public string? ToEmail { get; set; }
    public string? OtpCode { get; set; }
    public int ExpiredTime { get; set; }
    public string? TimeMeasureUnit { get; set; }
}
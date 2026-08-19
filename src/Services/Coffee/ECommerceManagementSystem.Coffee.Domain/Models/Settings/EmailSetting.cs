namespace ECommerceManagementSystem.Coffee.Domain.Models.Settings;

public class EmailSetting
{
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int OtpExpiryMinutes { get; set; } = 10;
    public bool EnableEmailSending { get; set; } = true;
}
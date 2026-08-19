namespace ECommerceManagementSystem.Coffee.Domain.Models.Settings;

public class BrandSetting
{
    public bool EnabledForgotPasswordFunction { get; set; }
    public string FrontEndAuthPath { get; set; }
    public bool EnabledSendEmailFunction { get; set; }
    public string SendGridApiKey { get; set; }
    public string SendGridFromEmail { get; set; }
    public string SendGridFromName { get; set; }
    public bool EnableOgPostFunction { get; set; }
    public string FrontEndPostPath { get; set; }
    
    public string FrontEndUrl { get; set; }
    public string MainColor { get; set; }
    
}
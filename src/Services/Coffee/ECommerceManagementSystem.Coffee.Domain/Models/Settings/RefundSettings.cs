using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Settings;

public class RefundSettings
{
    
    public ERefundMode DefaultMode { get; set; } = ERefundMode.Manual;
    public int ManualRefundSLA { get; set; } = 3; // Days
    public bool AutomaticRefundEnabled { get; set; } = false;
    public int AutomaticRefundRetryAttempts { get; set; } = 3;
    public int AutomaticRefundRetryDelayMinutes { get; set; } = 30;
    public bool RequireCustomerConfirmation { get; set; } = true;
    public bool SendReminderEmails { get; set; } = true;
    public int ReminderIntervalHours { get; set; } = 24;
}
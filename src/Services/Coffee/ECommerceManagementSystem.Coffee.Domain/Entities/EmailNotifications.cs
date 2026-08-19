using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class EmailNotifications : EntityAuditBase<Guid>
{
    public required Guid OrderId { get; set; }
    public EEmailType EmailType  { get; set; }
    public required string RecipientEmail  { get; set; }
    public required string Subject { get; set; }
    public string? EmailBody { get; set; }
    public EEmailStatus Status { get; set; }
    public DateTime? SentAt  { get; set; }
    public DateTime? FailedAt  { get; set; }
    public string? ErrorMessage  { get; set; }
    public int RetryCount  { get; set; }
    
    public virtual Orders? Order { get; set; }
}
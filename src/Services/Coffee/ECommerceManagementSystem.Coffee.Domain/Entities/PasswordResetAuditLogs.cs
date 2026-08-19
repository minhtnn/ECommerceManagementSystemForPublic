
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class PasswordResetAuditLogs : EntityAuditBase<Guid>
{
    public required Guid AccountId { get; set; }
    public required string Action { get; set; }
    public string? PartialToken { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public required bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Metadata { get; set; }
    public virtual Accounts Account { get; set; } = null!;
}
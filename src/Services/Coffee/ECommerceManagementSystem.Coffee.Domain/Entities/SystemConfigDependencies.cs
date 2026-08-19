using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class SystemConfigDependencies : EntityAuditBase<Guid>
{
    public required Guid TriggerKeyId { get; set; }
    public required string TriggerValue { get; set; }
    public required Guid DependentKeyId { get; set; }

    public virtual SystemConfigKeys TriggerKey { get; set; } = null!;
    public virtual SystemConfigKeys DependentKey { get; set; } = null!;
}
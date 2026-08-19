using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class SystemConfigValues : EntityAuditBase<Guid>
{
    public required Guid ConfigKeyId { get; set; }
    public string? Value { get; set; }

    public virtual SystemConfigKeys ConfigKey { get; set; } = null!;
}
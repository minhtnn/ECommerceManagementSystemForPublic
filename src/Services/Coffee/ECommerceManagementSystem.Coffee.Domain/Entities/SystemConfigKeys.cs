using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class SystemConfigKeys : EntityAuditBase<Guid>
{
    public required string Key { get; set; }
    public required string Title { get; set; }
    public required EConfigDataType DataType { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsSecure { get; set; } = false;
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; } = 0;

    public virtual List<SystemConfigValues> ConfigValues { get; set; } = new();
    public virtual List<SystemConfigDependencies> TriggerDependencies { get; set; } = new();
    public virtual List<SystemConfigDependencies> DependentDependencies { get; set; } = new();
}
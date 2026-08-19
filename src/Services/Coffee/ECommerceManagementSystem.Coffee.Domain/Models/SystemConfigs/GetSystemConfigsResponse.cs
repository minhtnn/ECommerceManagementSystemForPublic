
namespace ECommerceManagementSystem.Coffee.Domain.Models.SystemConfigs;

public class GetSystemConfigsResponse
{
    public Guid Id { get; set; }
    public string Key { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string DataType { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSecure { get; set; } = false;
    public string? DefaultValue { get; set; }
    public string? Value { get; set; }
    public int DisplayOrder { get; set; }
    public List<SystemConfigDependencyResponse> Dependencies { get; set; } = new();
}

public class SystemConfigDependencyResponse
{
    public Guid Id { get; set; }
    public Guid TriggerKeyId { get; set; }
    public string TriggerKey { get; set; } = null!;
    public string TriggerValue { get; set; } = null!;
}
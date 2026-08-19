using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.CreateSystemConfig;

public class CreateSystemConfigCommand : IRequest<ApiResponse>
{
    // Key info
    public required string Key { get; set; }
    public required string Title { get; set; }
    public required EConfigDataType DataType { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsSecure { get; set; } = false;
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public string? Value { get; set; }

    // Dependencies (optional)
    public List<SystemConfigDependencyRequest>? Dependencies { get; set; }
}

public class SystemConfigDependencyRequest
{
    public required Guid TriggerKeyId { get; set; }
    public required string TriggerValue { get; set; }
}
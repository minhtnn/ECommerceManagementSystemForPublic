using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.SystemConfigs.Command.UpdateSystemConfig;

public class UpdateSystemConfigCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    // Key info (Key và DataType không cho đổi sau khi tạo)
    public required string Title { get; set; }
    public string? Description { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSecure { get; set; } = false;
    public string? DefaultValue { get; set; }
    public int DisplayOrder { get; set; }

    // Value — null = giữ nguyên, empty string = xóa value
    public string? Value { get; set; }
    public bool ClearValue { get; set; } = false;

    // Dependencies — replace toàn bộ danh sách hiện tại
    public List<SystemConfigDependencyRequest>? Dependencies { get; set; }
}

public class SystemConfigDependencyRequest
{
    public required Guid TriggerKeyId { get; set; }
    public required string TriggerValue { get; set; }
}
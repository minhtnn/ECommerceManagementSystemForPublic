using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.CreateProductCategory;

public class CreateProductCategoryCommand : IRequest<ApiResponse>
{
    public Guid? ParentProductCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public IFormFile? Image { get; set; }
    public ECategoryStatus Status { get; set; } = ECategoryStatus.Active;
}
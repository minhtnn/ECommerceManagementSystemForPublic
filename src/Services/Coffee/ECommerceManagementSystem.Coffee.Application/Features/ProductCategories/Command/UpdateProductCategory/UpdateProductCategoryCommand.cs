using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Command.UpdateProductCategory;

public class UpdateProductCategoryCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public IFormFile? Image { get; set; }
    public ECategoryStatus Status { get; set; }
}
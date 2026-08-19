using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategoryById;

public class GetProductCategoryByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string TimeZone {get;set;}
}
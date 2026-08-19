using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProductById;

public class GetPublicProductByIdQuery : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public required string TimeZone {get;set;}
}

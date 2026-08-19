using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Query.GetProductById;

public class GetProductByIdQuery : IRequest<ApiResponse>
{
    public Guid ProductId { get; set; }
    public required string TimeZone {get;set;}
}
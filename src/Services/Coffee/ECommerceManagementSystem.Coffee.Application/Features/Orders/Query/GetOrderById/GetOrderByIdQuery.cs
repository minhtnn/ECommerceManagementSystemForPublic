using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetOrderById;

public class GetOrderByIdQuery : IRequest<ApiResponse>
{
    public Guid OrderId { get; set; }
    public required string TimeZone { get; set; }
}
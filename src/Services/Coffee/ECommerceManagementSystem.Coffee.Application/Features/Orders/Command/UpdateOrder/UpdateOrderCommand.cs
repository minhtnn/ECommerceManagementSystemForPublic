using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.UpdateOrder;

public class UpdateOrderCommand : IRequest<ApiResponse>
{
    public Guid OrderId { get; set; }
    public string? ShippingAddress { get; set; }
    public string? ShippingContact { get; set; }
    public string? CustomerNote { get; set; }
    public EOrderStatus? NewOrderStatus { get; set; }
    public string? CancelReason { get; set; }
}
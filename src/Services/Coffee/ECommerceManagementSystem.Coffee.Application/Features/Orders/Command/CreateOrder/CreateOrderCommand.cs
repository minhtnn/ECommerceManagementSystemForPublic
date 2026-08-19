using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Command.CreateOrder;

public class CreateOrderCommand : IRequest<ApiResponse>
{
    public Guid BrandPaymentMethodId { get; set; }
    public Guid CartId { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingContact { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }
    public required string TimeZone {get;set;}
}
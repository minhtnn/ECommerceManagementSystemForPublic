using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetCustomerOrders;

public class GetCustomerOrdersQuery : IRequest<ApiResponse>
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool IsAsc { get; set; } = false;
    public EOrderStatus? OrderStatus { get; set; }
    public EPaymentStatus? PaymentStatus { get; set; }
    public string? SearchKeyword { get; set; }
    public required string TimeZone { get; set; }
}
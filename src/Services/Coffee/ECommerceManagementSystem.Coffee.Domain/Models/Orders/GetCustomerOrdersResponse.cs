using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Orders;

public class GetCustomerOrdersResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public EOrderStatus OrderStatus { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedDate { get; set; }
}
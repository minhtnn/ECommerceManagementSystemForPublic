using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Orders;

public class CreateOrderResponse
{
    public Guid OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; } 
    public EOrderStatus OrderStatus { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedDate { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Orders;

public class GetOrderByIdResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public EOrderStatus OrderStatus { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmountWithoutDiscount { get; set; }
    public decimal TotalOrderDiscount { get; set; }
    public decimal TotalOrderShippingFee { get; set; }
    public decimal TotalAmount { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public string ShippingContact { get; set; } = string.Empty;
    public string? CustomerNote { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    
    // Customer info (for BrandAdmin)
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    
    // Items
    public List<OrderItemDetailResponse> Items { get; set; } = new();
    
    // Payments
    public List<OrderPaymentResponse> Payments { get; set; } = new();
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
}

public class OrderItemDetailResponse
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal TotalPriceSnapshot { get; set; }
}

public class OrderPaymentResponse
{
    public Guid Id { get; set; }
    public string PaymentMethodCodeSnapshot { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public EPaymentStatus PaymentStatus { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
}
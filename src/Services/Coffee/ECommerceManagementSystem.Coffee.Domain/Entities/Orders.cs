using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Orders : EntityAuditBase<Guid>
{
    public required Guid CustomerId  { get; set; }
    public required string Code {get; set;}
    public EOrderStatus OrderStatus  {get; set;}
    public EPaymentStatus  PaymentStatus  {get; set;}
    public decimal TotalAmountWithoutDiscount {get; set;}
    public decimal TotalOrderDiscount  {get; set;}
    public decimal TotalOrderShippingFee  {get; set;}
    public decimal TotalAmount {get; set;}
    public required string ShippingAddress { get; set; }
    public required string ShippingContact { get; set; }
    public string? CustomerNote  {get; set;}
    public string? PaymentUrl { get; set; }
    public string? QrCode { get; set; }
    public bool IsAggregated { get; set; } = false;
    public DateTime? AggregatedAt { get; set; }
    public Guid? CancelledBy { get; set; }
    public ERole? CancelledByRole { get; set; }
    public string? CancelReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    
    public virtual Customers? Customer { get; set; }
    public virtual RefundRequests? RefundRequest { get; set; }
    public virtual List<OrderHistoryStatus> OrderHistoryStatuses { get; set; } = new List<OrderHistoryStatus>();
    public virtual List<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();
    public virtual List<Payments> Payments { get; set; } = new List<Payments>();
    public virtual List<AppliedOrderPromotions> AppliedOrderPromotions { get; set; } =
        new List<AppliedOrderPromotions>();
    public virtual List<EmailNotifications> EmailNotifications { get; set; } = new List<EmailNotifications>();
}
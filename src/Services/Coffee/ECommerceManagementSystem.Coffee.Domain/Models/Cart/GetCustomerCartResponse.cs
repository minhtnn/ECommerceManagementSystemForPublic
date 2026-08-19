using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Cart;

public class GetCustomerCartResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CartName { get; set; } = "Giỏ hàng chính";
    public bool IsActive { get; set; } = true;
    public decimal TotalAmountWithoutDiscount {get; set;}
    public decimal TotalOrderDiscount {get; set;}
    public decimal TotalOrderShippingFee {get; set;}
    public decimal TotalAmount {get; set;}
    public string? CustomerNote {get; set;}
    public DateTime CreatedDate {get; set;}
    public DateTime LastModifiedDate {get; set;}
    public List<GetCustomerCartItemsResponse> Items { get; set; } = new();
    public List<GetCustomerCartAppliedPromotionsResponse> AppliedPromotions { get; set; } = new();
}

public class GetCustomerCartItemsResponse
{
    public Guid GetCustomerCartId { get; set; }
    public Guid ProductId { get; set; }
    public required string ProductNameSnapshot {get; set;}
    public string ProductImageUrlSnapshot {get; set;}
    public int Quantity {get; set;}
    public decimal UnitPriceSnapshot {get; set;}
    public decimal TotalAmountSnapshot {get; set;}
    public bool IsGiftItem { get; set; } = false;
    public Guid? PromotionId { get; set; }
}

public class GetCustomerCartAppliedPromotionsResponse
{
    public Guid GetCustomerCartId { get; set; }
    public Guid PromotionId { get; set; }
    public required string PromotionRuleCode {get; set;}
    public required string PromotionRuleNameSnapshot { get; set; }
    public decimal DiscountAmountApplied {get; set;}
    public decimal CreatedDate {get; set;}
    public EStackingSlot StackingSlot {get; set;}
}
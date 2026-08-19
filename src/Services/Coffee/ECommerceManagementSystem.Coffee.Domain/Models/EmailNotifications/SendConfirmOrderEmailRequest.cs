using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Domain.Models.EmailNotifications;

public class SendConfirmOrderEmailRequest
{
    public string BrandLogoBase64 { get; set; }
    public string BrandName { get; set; }
    public string CustomerName { get; set; }
    public string? FromEmail { get; set; }
    public required string CustomerEmail { get; set; }
    public string? ReceiveNumber { get; set; }
    public string? ReceiveAddress { get; set; }
    public DateTime? OrderDate { get; set; }
    public required string OrderCode { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<SendConfirmOrderDetailEmailRequest>? OrderDetails { get; set; }
}

public class SendConfirmOrderDetailEmailRequest
{
    public string ProductName { get; set; }
    public decimal Quantity { get; set; }
    public string? ProductImagePath { get; set; }
    public string? ProductImageBase64 { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal TotalPriceSnapshot { get; set; }
    public bool IsGiftItem { get; set; }
}
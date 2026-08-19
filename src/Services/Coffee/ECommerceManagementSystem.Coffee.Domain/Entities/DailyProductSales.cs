using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

public class DailyProductSales : EntityAuditBase<Guid>
{
    public required Guid ProductId { get; set; }
    public required string ProductNameSnapshot { get; set; } // snapshot tại thời điểm aggregate
    public string? ProductImagePath { get; set; } // snapshot tại thời điểm aggregate
    public required DateOnly SaleDate { get; set; }

    public int TotalQuantitySold { get; set; }        // SUM(Quantity) — chỉ non-gift
    public int TotalGiftQuantity { get; set; }         // SUM(Quantity) WHERE IsGiftItem = true
    public decimal TotalRevenueGross { get; set; }     // SUM(TotalPriceSnapshot) non-gift
    public int TotalOrderCount { get; set; }           // số đơn Completed có sản phẩm này

    public virtual Products? Product { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class BrandDailySummary : EntityAuditBase<Guid>
{
    public required Guid BrandId { get; set; }
    public required DateOnly SummaryDate { get; set; }

    // Thông số 7, 8, 9 — tất cả đơn Completed
    public decimal TotalRevenueGross { get; set; }      // SUM(TotalAmountWithoutDiscount)
    public decimal TotalDiscount { get; set; }           // SUM(TotalOrderDiscount)
    public decimal TotalRevenueNet { get; set; }         // SUM(TotalAmount)
    public int TotalOrderCount { get; set; }             // COUNT đơn Completed

    // Thông số 10, 11 — chỉ đơn Delivered
    public decimal TotalRevenueGrossDelivered { get; set; }
    public decimal TotalDiscountDelivered { get; set; }
    public decimal TotalRevenueNetDelivered { get; set; }
    public int TotalOrderCountDelivered { get; set; }
    public int TotalQuantitySoldDelivered { get; set; } // SUM(OrderDetails.Quantity) non-gift

    public virtual Brands? Brand { get; set; }
}
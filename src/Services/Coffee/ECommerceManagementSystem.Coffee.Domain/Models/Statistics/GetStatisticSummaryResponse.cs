namespace ECommerceManagementSystem.Coffee.Domain.Models.Statistics;

public class GetStatisticSummaryResponse
{
    public decimal TotalRevenueGross { get; set; }
    public int TotalOrderCount { get; set; }
    public int TotalQuantitySold { get; set; }
    public int TotalGiftQuantity { get; set; }

    public decimal TotalDiscountIssued { get; set; }
    public int TotalOrdersWithPromo { get; set; }
    public int TotalPromotionCount { get; set; }
}
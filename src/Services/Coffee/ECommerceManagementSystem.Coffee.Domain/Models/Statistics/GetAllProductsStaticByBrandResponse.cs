namespace ECommerceManagementSystem.Coffee.Domain.Models.Statistics;

public class GetAllProductsStaticByBrandResponse
{
    public required Guid ProductId { get; set; }
    public required string ProductNameSnapshot { get; set; }
    public string? ProductImagePath { get; set; }
    public string? ProductImageUrl { get; set; }
    public required DateOnly SaleDate { get; set; }
    public int TotalQuantitySold { get; set; }
    public int TotalGiftQuantity { get; set; }
    public decimal TotalRevenueGross { get; set; }
    public int TotalOrderCount { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Products;

public class GetProductsResponse
{
    public Guid Id { get; set; }
    public required Guid ProductCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public EProductSellType  ProductSellType { get; set; }
    public EProductStatus Status { get; set; }
    public int StockQuantity  { get; set; }
    public string MainImagePath { get; set; } = string.Empty;
    public string MainImageUrl { get; set; } = string.Empty;
    public string? MainImageAltText { get; set; }
}
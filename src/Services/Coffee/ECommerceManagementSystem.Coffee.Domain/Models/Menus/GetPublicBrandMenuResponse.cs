using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Menus;



public class GetPublicMenuProductCategoryResponse
{
    public Guid Id { get; set; }
    public Guid ParentProductCategoryId { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public string Name { get; set; }
    public bool IsSelected { get; set; }
    public int DisplayOrder { get; set; }
    public int ProductCount { get; set; }
    public int TotalProductCount { get; set; }
    public List<GetPublicMenuProductCategoryResponse> Children { get; set; } = new();
    // public List<GetPublicMenuProductResponse> GetMenuProductsResponse { get; set; } = new();
}

public class GetPublicMenuProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int StockQuantity  { get; set; }
    // public int DisplayOrder { get; set; }
    public Guid ProductCategoryId { get; set; }
    // public EProductStatus Status { get; set; }
    public List<GetPublicMenuProductImageResponse>  Images { get; set; } = new();
}

public class GetPublicMenuProductImageResponse
{
    public Guid Id { get; set; }
    public string? AltText { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
    public bool IsMainImage { get; set; }
}
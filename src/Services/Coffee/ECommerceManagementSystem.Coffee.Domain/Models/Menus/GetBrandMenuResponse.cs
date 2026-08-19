using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Menus;

public class GetBrandMenuResponse
{
    public GetMenuProductCategoryResponse SelectedCategory { get; set; }
    public ICollection<GetMenuProductCategoryResponse>  ProductCategoriesTree { get; set; }
    public List<GetMenuProductResponse>  Products { get; set; } 
    public int TotalProducts { get; set; }
}

public class GetMenuProductCategoryResponse
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
    public List<GetMenuProductCategoryResponse> Children { get; set; } = new();
    public List<GetMenuProductResponse> GetMenuProductsResponse { get; set; } = new();
}

public class GetMenuProductResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int StockQuantity  { get; set; }
    public EProductStatus Status { get; set; }
    public List<GetMenuProductImageResponse>  Images { get; set; } = new();
}

public class GetMenuProductImageResponse
{
    public Guid Id { get; set; }
    public string? AltText { get; set; }
    public string? Path { get; set; }
    public string? Url { get; set; }
    public bool IsMainImage { get; set; }
}
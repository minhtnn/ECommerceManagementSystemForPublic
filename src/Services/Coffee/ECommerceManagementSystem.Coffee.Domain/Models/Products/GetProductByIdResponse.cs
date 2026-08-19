using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Products;

public class GetProductByIdResponse
{
    public Guid Id { get; set; }
    public string ProductCategoryName { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public EProductSellType  ProductSellType { get; set; }
    public EProductStatus Status { get; set; }
    public int? DisplayOrder { get; set; }
    public int StockQuantity { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public virtual List<GetProductByIdImagesResponse>? GetProductImagesResponse { get; set; }
    public virtual List<GetProductByIdSideAttributesResponse>? GetProductSideAttributesResponse { get; set; }
}

public class GetProductByIdImagesResponse
{
    public Guid Id { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public bool IsMainImage { get; set; }
}

public class GetProductByIdSideAttributesResponse
{
    public Guid Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}
using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Command.CreateProduct;

public class CreateProductCommand : IRequest<ApiResponse>
{
    public required Guid ProductCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? DisplayOrder { get; set; }
    public EProductSellType  ProductSellType { get; set; }
    public EProductStatus  Status { get; set; }
    public int StockQuantity  { get; set; }
    public IFormFileCollection? ImageFiles { get; set; }
    public string? ImageMetadataJson { get; set; }
    [JsonIgnore]
    public List<CreateProductImageMetadata>? CreateProductImageMetadata => 
        string.IsNullOrEmpty(ImageMetadataJson) 
            ? null 
            : JsonSerializer.Deserialize<List<CreateProductImageMetadata>>(ImageMetadataJson);
    public List<CreateProductSideAttributes>? SideAttibutes { get; set; }
}
public class CreateProductImageMetadata
{
    public string? AltText { get; set; }
    public bool IsMainImage { get; set; }
}

public class CreateProductSideAttributes
{
    public required string Key {get; set;}
    public required string Value {get; set;}
}
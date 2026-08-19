using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Products.Command.UpdateProduct;

public class UpdateProductCommand : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
    public decimal? Price { get; set; }
    public EProductStatus Status { get; set; }
    public EProductSellType ProductSellType { get; set; }
    public int StockQuantity { get; set; }
    public IFormFileCollection? NewImageFiles { get; set; }
    public string? ExistingImageMetadataJson { get; set; }

    [JsonIgnore]
    public List<UpdateExistingImageMetadata>? ExistingImageMetadata =>
        string.IsNullOrEmpty(ExistingImageMetadataJson)
            ? null
            : JsonSerializer.Deserialize<List<UpdateExistingImageMetadata>>(ExistingImageMetadataJson);

    public string? NewImageMetadataJson { get; set; }

    [JsonIgnore]
    public List<UpdateProductImageMetadata>? UpdateNewImageMetadata =>
        string.IsNullOrEmpty(NewImageMetadataJson)
            ? null
            : JsonSerializer.Deserialize<List<UpdateProductImageMetadata>>(NewImageMetadataJson);

    public List<Guid>? ExistingImageIds { get; set; }
    public string? SideAttributesJson { get; set; }

    [JsonIgnore]
    public List<UpdateProductSideAttributes>? SideAttibutes =>
        string.IsNullOrEmpty(SideAttributesJson)
            ? null
            : JsonSerializer.Deserialize<List<UpdateProductSideAttributes>>(SideAttributesJson);
}

public class UpdateProductImageMetadata
{
    public string? AltText { get; set; }
    public bool IsMainImage { get; set; }
}

public class UpdateProductSideAttributes
{
    public required string Key { get; set; }
    public required string Value { get; set; }
}

public class UpdateExistingImageMetadata
{
    public Guid Id { get; set; }
    public string? AltText { get; set; }
    public bool IsMainImage { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Products : EntityAuditBase<Guid>
{
    public required Guid ProductCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? FullName { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public EProductSellType  ProductSellType { get; set; }
    public EProductStatus  Status { get; set; }
    public int StockQuantity  { get; set; }
    public int DisplayOrder { get; set; }
    
    public virtual ProductCategories? ProductCategory { get; set; }
    public virtual List<ProductImages> ProductImages { get; set; } = new List<ProductImages>();
    public virtual List<ProductSideAttributes> ProductSideAttributes { get; set; } = new List<ProductSideAttributes>();
    public virtual List<OrderDetails> OrderDetails { get; set; } = new List<OrderDetails>();
}
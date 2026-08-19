using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class ProductCategories : EntityAuditBase<Guid>
{
    public required Guid BrandId { get; set; }
    public Guid? ParentProductCategoryId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int Level  { get; set; }
    public bool IsLeafOnly { get; set; }
    public bool IsDeletable { get; set; }
    public string? ImageUrl { get; set; }
    public ECategoryStatus Status { get; set; }

    public virtual ProductCategories? Parent { get; set; }
    public virtual Brands Brand { get; set; } = null!;
    public virtual List<ProductCategories>? Childrens { get; set; } = new List<ProductCategories>();
    public virtual List<Products>? Products { get; set; } = new List<Products>();
}
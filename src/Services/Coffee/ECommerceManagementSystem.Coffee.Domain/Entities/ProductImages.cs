using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class ProductImages : EntityAuditBase<Guid>
{
    public required Guid ProductId { get; set; }
    public string? ImageUrl { get; set; }
    public string? AltText { get; set; }
    public bool IsMainImage { get; set; }
    
    public virtual Products? Product { get; set; }
}
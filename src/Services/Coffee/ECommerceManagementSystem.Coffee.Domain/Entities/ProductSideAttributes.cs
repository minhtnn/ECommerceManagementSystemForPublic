using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class ProductSideAttributes : EntityAuditBase<Guid>
{
    public required Guid ProductId { get; set; }
    public required string Key {get; set;}
    public required string Value {get; set;}
    public virtual Products? Product { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class BrandPaymentMethods : EntityAuditBase<Guid>
{
    public Guid BrandId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Configuration {get; set;}
    public virtual Brands? Brand { get; set; }
    public virtual PaymentMethods? PaymentMethods { get; set; }
}
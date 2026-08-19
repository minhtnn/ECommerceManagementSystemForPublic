using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class PaymentMethods : EntityAuditBase<Guid>
{
    public required string Code {get; set;}
    public required string Name {get; set;}
    public string? ImageUrl {get; set;}
    public string? ConfigurationSchema { get; set; }
    public EPaymentMethodStatus  Status {get; set;}

    public virtual List<Payments> Payments { get; set; } = new List<Payments>();
    public virtual List<BrandPaymentMethods> BrandPaymentMethods { get; set; } = new List<BrandPaymentMethods>();

}
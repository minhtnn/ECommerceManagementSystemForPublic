using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class BrandAccounts : EntityAuditBase<Guid>
{
    public Guid BrandId { get; set; }
    public Guid AccountId { get; set; }
    
    public virtual Accounts? Account { get; set; }
    public virtual Brands Brand { get; set; }
}
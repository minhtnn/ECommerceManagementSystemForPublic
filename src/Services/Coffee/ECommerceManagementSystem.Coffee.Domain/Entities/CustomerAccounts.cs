using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class CustomerAccounts : EntityAuditBase<Guid>
{
    public required Guid CustomerId {get;set;}
    public required Guid AccountId {get;set;}
    
    public virtual Customers? Customer {get;set;}
    public virtual Accounts? Account {get;set;}
}
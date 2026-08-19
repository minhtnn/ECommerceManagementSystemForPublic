using ECommerceManagementSystem.Coffee.Domain.Entities.Commons.Interface;

namespace ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

public abstract class EntityAuditBase<T> : EntityBase<T>, IAuditable
{
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
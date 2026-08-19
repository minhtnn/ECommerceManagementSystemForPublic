using ECommerceManagementSystem.Coffee.Domain.Entities.Commons.Interface;

namespace ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

public class EntityBase<TKey> : IEntityBase<TKey>
{
    public TKey Id { get; set; }
}
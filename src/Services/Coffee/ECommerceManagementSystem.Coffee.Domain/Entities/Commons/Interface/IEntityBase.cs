namespace ECommerceManagementSystem.Coffee.Domain.Entities.Commons.Interface;

public interface IEntityBase<T>
{
    T Id { get; set; }
}
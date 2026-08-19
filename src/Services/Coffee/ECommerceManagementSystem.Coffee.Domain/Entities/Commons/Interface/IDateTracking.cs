namespace ECommerceManagementSystem.Coffee.Domain.Entities.Commons.Interface;

public interface IDateTracking
{
    DateTime CreatedDate { get; set; }
    DateTime? LastModifiedDate { get; set; } 
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class CustomerAddresses : EntityAuditBase<Guid>
{
    public Guid CustomerId { get; set; }
    public required string Receiver {get; set;}
    public required string Address {get; set;}
    public required string ShippingContact {get; set;}
    public double Latitude {get; set;}
    public double Longitude {get; set;}
    public bool IsPrimary {get; set;}
    
    public virtual Customers? Customer {get; set;}
}
namespace ECommerceManagementSystem.Coffee.Domain.Models.Customer;

public class GetCustomerAddressByIdResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public required string Receiver {get; set;}
    public required string Address {get; set;}
    public required string ShippingContact {get; set;}
    public double Latitude {get; set;}
    public double Longitude {get; set;}
    public bool IsPrimary {get; set;}
    public DateTime CreatedDate {get; set;}
    public DateTime? LastModifiedDate {get; set;}
}
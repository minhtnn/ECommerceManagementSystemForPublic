using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Customers : EntityAuditBase<Guid>
{
    public required Guid BrandId { get; set; }
    public required string FullName {get; set;}
    public required string Email {get; set;}
    public string? PhoneNumber {get; set;}
    public string? AvatarUrl {get; set;}
    public virtual Brands? Brand { get; set; }
    public virtual List<Orders> Orders { get; set; } = new List<Orders>();
    public virtual List<CustomerAddresses> CustomerAddresses { get; set; } = new List<CustomerAddresses>();
    public virtual List<CustomerAccounts> CustomerAccounts { get; set; } = new List<CustomerAccounts>();
}
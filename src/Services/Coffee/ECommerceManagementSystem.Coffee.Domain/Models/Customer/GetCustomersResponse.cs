using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Customer;

public class GetCustomersResponse
{
    public Guid Id { get; set; }
    public string? AvatarUrl { get; set; }
    public required string FullName {get; set;}
    public required string Email {get; set;}
    public required string PhoneNumber {get; set;}
    public EAccountStatus Status { get; set; }
}
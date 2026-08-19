namespace ECommerceManagementSystem.Coffee.Domain.Models.Authentication;

public class AccountDetailResponse
{
    public string? ImageUrl { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Address { get; set; }
}
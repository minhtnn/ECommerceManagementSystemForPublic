namespace ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;

public class GetBrandPaymentMethodsResponse
{
    public Guid Id { get; set; }
    public Guid PaymentMethodId { get; set; }
    public required string Name {get; set;}
    public string? ImagePath {get; set;}
    public string? ImageUrl {get; set;}
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
}
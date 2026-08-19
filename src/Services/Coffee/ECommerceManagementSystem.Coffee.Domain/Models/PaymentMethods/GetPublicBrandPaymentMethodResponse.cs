namespace ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;

public class GetPublicBrandPaymentMethodResponse
{
    public Guid Id { get; set; }
    public Guid PaymentMethodId { get; set; }
    public string BrandPaymentMethodCode  { get; set; }
    public required string Name {get; set;}
    public string? ImagePath {get; set;}
    public string? ImageUrl {get; set;}
    public bool IsDefault {get; set;}
}
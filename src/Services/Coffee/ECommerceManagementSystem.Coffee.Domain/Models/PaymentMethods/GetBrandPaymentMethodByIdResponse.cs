namespace ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;

public class GetBrandPaymentMethodByIdResponse
{
    public Guid Id { get; set; }
    public Guid PaymentMethodId { get; set; }
    public required string Name { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? BrandConfiguration { get; set; }
    public string? SystemConfiguration { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
}
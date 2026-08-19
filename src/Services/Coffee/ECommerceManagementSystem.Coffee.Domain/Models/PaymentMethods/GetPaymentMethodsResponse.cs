using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.PaymentMethods;

public class GetPaymentMethodsResponse
{
    public Guid Id { get; set; }
    public required string Code {get; set;}
    public required string Name {get; set;}
    public string? ImagePath {get; set;}
    public string? ImageUrl {get; set;}
    public string? SystemConfigurationSchema { get; set; }
    public EPaymentMethodStatus Status {get; set;}
}
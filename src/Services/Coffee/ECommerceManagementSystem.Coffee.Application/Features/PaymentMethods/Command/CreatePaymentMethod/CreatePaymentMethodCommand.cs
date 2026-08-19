using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreatePaymentMethod;

public class CreatePaymentMethodCommand : IRequest<ApiResponse>
{
    public required string Code {get; set;}
    public required string Name {get; set;}
    public IFormFile? Image {get; set;}
    public string? ConfigurationSchema { get; set; }
    public EPaymentMethodStatus Status {get; set;}
}
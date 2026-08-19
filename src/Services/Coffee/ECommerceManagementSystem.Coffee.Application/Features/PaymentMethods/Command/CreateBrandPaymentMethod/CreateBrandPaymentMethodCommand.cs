using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Command.CreateBrandPaymentMethod;

public class CreateBrandPaymentMethodCommand : IRequest<ApiResponse>
{
    public Guid PaymentMethodId { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Configuration {get; set;}
}
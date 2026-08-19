using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetBrandPaymentMethodById;

public class GetBrandPaymentMethodByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string TimeZone {get;set;}
}
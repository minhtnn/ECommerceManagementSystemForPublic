using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPaymentMethodById;

public class GetPaymentMethodByIdQuery : IRequest<ApiResponse>
{
    public Guid PaymentMethodId { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PaymentMethods.Query.GetPublicBrandPaymentMethods;

public class GetPublicBrandPaymentMethodsQuery : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
}
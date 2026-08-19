using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Orders.Query.GetPaymentLink;

public class GetPaymentLinkQuery : IRequest<ApiResponse>
{
    public Guid OrderId { get; set; }
}
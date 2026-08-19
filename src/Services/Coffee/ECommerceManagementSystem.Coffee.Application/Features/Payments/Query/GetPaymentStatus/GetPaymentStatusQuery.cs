using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Query.GetPaymentStatus;

public class GetPaymentStatusQuery : IRequest<ApiResponse>
{
    public Guid OrderId { get; set; }
    public required string TimeZone { get; set; }
}
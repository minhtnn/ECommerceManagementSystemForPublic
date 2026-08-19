using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.CancelPayment;

public class CancelPaymentCommand : IRequest<ApiResponse>
{
    public Guid OrderId { get; set; }
    public string? CancelReason { get; set; }
}
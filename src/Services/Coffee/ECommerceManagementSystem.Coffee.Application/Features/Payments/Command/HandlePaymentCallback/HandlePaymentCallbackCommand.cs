using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;
using PayOS.Models.Webhooks;

namespace ECommerceManagementSystem.Coffee.Application.Features.Payments.Command.HandlePaymentCallback;

/// <summary>
/// Command to handle payment gateway callbacks/webhooks
/// </summary>
public class HandlePaymentCallbackCommand : IRequest<ApiResponse>
{
    /// <summary>
    /// Webhook data from PayOS
    /// </summary>
    public Webhook? CallbackData { get; set; }
    
    /// <summary>
    /// Payment gateway code to resolve service
    /// Should match PaymentMethods.Code (e.g., "PAYOS")
    /// </summary>
    public string PaymentGatewayCode { get; set; } = string.Empty;
}
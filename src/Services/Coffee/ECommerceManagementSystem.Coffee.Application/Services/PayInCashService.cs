using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using PayOS.Models.Webhooks;

namespace ECommerceManagementSystem.Coffee.Application.Services;

/// <summary>
/// Payment service for Cash on Delivery (COD)
/// No external API calls needed - payment completed on delivery
/// </summary>
public class PayInCashService : IPaymentGatewayService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Payment method code - must match PaymentMethods.Code in database
    /// </summary>
    public string Code => "PayInCash";

    public PayInCashService(ILogger logger)
    {
        _logger = logger;
    }

    public Task<PaymentGatewayResult> CreatePaymentAsync(
        Orders order,
        Payments payment,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        _logger.Information(
            "💵 COD payment selected for order {OrderCode}, Amount: {Amount}",
            order.Code,
            order.TotalAmount);

        // COD doesn't need payment URL - customer pays on delivery
        // Generate internal transaction ID for tracking
        var transactionId = $"COD-{order.Code}-{DateTime.UtcNow:yyyyMMddHHmmss}";

        return Task.FromResult(new PaymentGatewayResult
        {
            Success = true,
            PaymentUrl = null, // No online payment needed
            QrCode = null,
            TransactionId = transactionId
        });
    }

    public Task<PaymentVerificationResult> VerifyPaymentAsync(
        Webhook callbackData,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        // COD doesn't have callbacks - payment verified manually on delivery
        _logger.Warning("VerifyPaymentAsync called on PayInCashService - COD doesn't support callbacks");
        
        return Task.FromResult(new PaymentVerificationResult
        {
            IsValid = true,
            IsSuccess = true, // COD is considered successful when order is created
            ErrorMessage = "COD payment - no verification needed"
        });
    }
    
    public Task<PaymentRefundResult> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string reason,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.Information("COD refund request - will be handled manually offline");
    
        return Task.FromResult(new PaymentRefundResult
        {
            Success = true,
            RefundTransactionId = transactionId,
            RefundedAmount = amount,
            ActualRefundAmount = amount,
            RefundedAt = DateTime.UtcNow
        });
    }
}
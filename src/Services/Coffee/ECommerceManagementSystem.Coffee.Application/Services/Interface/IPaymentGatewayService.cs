using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using PayOS.Models.Webhooks;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

/// <summary>
/// Interface for payment gateway services
/// Each payment method (PayOS, COD, VNPay, etc.) implements this interface
/// </summary>
public interface IPaymentGatewayService
{
    /// <summary>
    /// Payment gateway code - must match PaymentMethods.Code in database
    /// Examples: "PAYOS", "PayInCash", "VNPAY", "MOMO"
    /// </summary>
    string Code { get; }

    /// <summary>
    /// Create payment URL or process payment
    /// </summary>
    /// <param name="order">Order entity with OrderDetails and Customer navigation loaded</param>
    /// <param name="payment">Payment entity</param>
    /// <param name="configuration">JSON configuration from BrandPaymentMethods.Configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment gateway result with URL, QR code, or transaction ID</returns>
    Task<PaymentGatewayResult> CreatePaymentAsync(
        Orders order,
        Payments payment,
        string configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify payment callback/webhook from gateway
    /// </summary>
    /// <param name="callbackData">Webhook data from PayOS</param>
    /// <param name="configuration">JSON configuration from BrandPaymentMethods.Configuration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result with validity and payment status</returns>
    Task<PaymentVerificationResult> VerifyPaymentAsync(
        Webhook callbackData,
        string configuration,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Refund a payment transaction
    /// </summary>
    Task<PaymentRefundResult> RefundPaymentAsync(
        string transactionId,
        decimal amount,
        string reason,
        string configuration,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result from payment gateway creation
/// </summary>
public class PaymentGatewayResult
{
    public bool Success { get; set; }
    
    /// <summary>
    /// Payment URL to redirect customer (for online payments)
    /// Null for COD or other offline methods
    /// </summary>
    public string? PaymentUrl { get; set; }
    
    /// <summary>
    /// QR code data URL (for QR-based payments like PayOS)
    /// </summary>
    public string? QrCode { get; set; }
    
    /// <summary>
    /// Error message if Success = false
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Transaction ID from payment gateway
    /// Used to track payment status and match callbacks
    /// </summary>
    public string? TransactionId { get; set; }
}

/// <summary>
/// Result from payment verification (callback/webhook)
/// </summary>
public class PaymentVerificationResult
{
    /// <summary>
    /// Whether the callback signature is valid
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Whether the payment was successful
    /// </summary>
    public bool IsSuccess { get; set; }
    
    /// <summary>
    /// Transaction ID from gateway
    /// </summary>
    public string? TransactionId { get; set; }
    
    /// <summary>
    /// Payment amount (for verification)
    /// </summary>
    public decimal? Amount { get; set; }
    
    /// <summary>
    /// Error message if IsValid = false
    /// </summary>
    public string? ErrorMessage { get; set; }
}

public class PaymentRefundResult
{
    public bool Success { get; set; }
    public string? RefundTransactionId { get; set; }
    public decimal RefundedAmount { get; set; }
    public decimal? RefundFee { get; set; }
    public decimal? ActualRefundAmount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? RefundedAt { get; set; }
}
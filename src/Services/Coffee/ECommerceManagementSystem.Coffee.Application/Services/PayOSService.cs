using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;

namespace ECommerceManagementSystem.Coffee.Application.Services.PaymentGateways;

/// <summary>
/// Payment service for PayOS gateway
/// Official PayOS SDK documentation: https://payos.vn/docs/
/// </summary>
public class PayOSService : IPaymentGatewayService
{
    private readonly ILogger _logger;

    /// <summary>
    /// Payment method code - must match PaymentMethods.Code in database
    /// </summary>
    public string Code => "PAYOS";

    public PayOSService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<PaymentGatewayResult> CreatePaymentAsync(
        Orders order,
        Payments payment,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        if (string.IsNullOrWhiteSpace(configuration))
        {
            _logger.Error("PayOS configuration is empty for order {OrderCode}", order.Code);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = "Cấu hình PayOS không hợp lệ"
            };
        }

        try
        {
            var config = JsonSerializer.Deserialize<PayOSSetting>(configuration);
            if (config == null || !IsValidConfig(config))
            {
                _logger.Error("Invalid PayOS configuration for order {OrderCode}", order.Code);
                return new PaymentGatewayResult
                {
                    Success = false,
                    ErrorMessage = "Cấu hình PayOS không đầy đủ"
                };
            }

            var payOSClient = CreatePayOSClient(config);

            // Generate unique numeric order code for PayOS
            var orderCode = GenerateOrderCode(order.Code);

            // Build payment request
            var paymentRequest = new CreatePaymentLinkRequest()
            {
                OrderCode = orderCode,
                Amount = (int)order.TotalAmount, // PayOS expects integer (VND)
                Description = $"Order {order.Code}",
                BuyerName = order.Customer?.FullName,
                BuyerEmail = order.Customer?.Email,
                BuyerPhone = order.ShippingContact,
                BuyerAddress = order.ShippingAddress,
                Items = order.OrderDetails?.Select(od => new PaymentLinkItem()
                {
                    Name = od.ProductNameSnapshot ?? "Product",
                    Quantity = od.Quantity,
                    Price = (int)od.UnitPriceSnapshot
                }).ToList() ?? new List<PaymentLinkItem>(),
                ReturnUrl = config.ReturnUrl,
                CancelUrl = config.CancelUrl,
                ExpiredAt = DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds()
            };

            // Generate signature using PayOS SDK
            paymentRequest.Signature = payOSClient.Crypto.CreateSignatureOfPaymentRequest(
                paymentRequest, 
                config.ChecksumKey);

            _logger.Information(
                "🔄 Creating PayOS payment - Order: {OrderCode}, Amount: {Amount}, OrderCodeNumeric: {OrderCodeNumeric}",
                order.Code, 
                order.TotalAmount,
                orderCode);

            // Call PayOS API to create payment link
            var response = await payOSClient.PaymentRequests.CreateAsync(paymentRequest);

            _logger.Information(
                "✅ PayOS payment created - PaymentLinkId: {PaymentLinkId}, CheckoutUrl: {CheckoutUrl}",
                response.PaymentLinkId,
                response.CheckoutUrl);

            return new PaymentGatewayResult
            {
                Success = true,
                PaymentUrl = response.CheckoutUrl,
                QrCode = response.QrCode,
                TransactionId = response.PaymentLinkId
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Error creating PayOS payment for order {OrderCode}", order.Code);
            return new PaymentGatewayResult
            {
                Success = false,
                ErrorMessage = $"Lỗi tạo thanh toán PayOS: {ex.Message}"
            };
        }
    }

    public async Task<PaymentVerificationResult> VerifyPaymentAsync(
        Webhook callbackData,
        string configuration,
        CancellationToken cancellationToken = default)
    {
        if (callbackData == null)
        {
            _logger.Error("Webhook data is null");
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Dữ liệu webhook không hợp lệ"
            };
        }

        if (string.IsNullOrWhiteSpace(configuration))
        {
            _logger.Error("PayOS configuration is empty");
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = "Cấu hình PayOS không hợp lệ"
            };
        }

        try
        {
            var config = JsonSerializer.Deserialize<PayOSSetting>(configuration);
            if (config == null || !IsValidConfig(config))
            {
                return new PaymentVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Cấu hình PayOS không đầy đủ"
                };
            }

            var payOSClient = CreatePayOSClient(config);

            // Validate required fields
            if (callbackData.Data == null ||
                string.IsNullOrEmpty(callbackData.Data.PaymentLinkId) ||
                callbackData.Data.OrderCode == null)
            {
                _logger.Warning("Missing required callback parameters");
                return new PaymentVerificationResult
                {
                    IsValid = false,
                    ErrorMessage = "Thiếu thông tin callback"
                };
            }

            _logger.Information(
                "🔐 Verifying PayOS webhook - PaymentLinkId: {PaymentLinkId}, Code: {Code}",
                callbackData.Data.PaymentLinkId,
                callbackData.Data.Code);

            // Verify signature using PayOS SDK
            if (!string.IsNullOrEmpty(callbackData.Signature))
            {
                try
                {
                    var isValidSignature = await payOSClient.Webhooks.VerifyAsync(callbackData);
                    
                    if (isValidSignature == null)
                    {
                        _logger.Warning("❌ Invalid PayOS webhook signature");
                        return new PaymentVerificationResult
                        {
                            IsValid = false,
                            ErrorMessage = "Chữ ký webhook không hợp lệ"
                        };
                    }
                    
                    _logger.Information("✅ PayOS webhook signature verified");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error verifying PayOS signature");
                    return new PaymentVerificationResult
                    {
                        IsValid = false,
                        ErrorMessage = "Lỗi xác thực chữ ký"
                    };
                }
            }

            // Check payment status
            var isSuccess = callbackData.Data.Code == "00";

            _logger.Information(
                "📊 Webhook verification result - IsSuccess: {IsSuccess}, Code: {Code}, Description: {Description}",
                isSuccess,
                callbackData.Data.Code,
                callbackData.Data.Description);

            return new PaymentVerificationResult
            {
                IsValid = true,
                IsSuccess = isSuccess,
                TransactionId = callbackData.Data.PaymentLinkId,
                Amount = callbackData.Data.Amount,
                ErrorMessage = isSuccess ? null : callbackData.Data.Description
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "❌ Error verifying PayOS payment callback");
            return new PaymentVerificationResult
            {
                IsValid = false,
                ErrorMessage = $"Lỗi xác thực callback: {ex.Message}"
            };
        }
    }

    

    /// <summary>
    /// Generate numeric order code for PayOS
    /// PayOS requires orderCode to be a positive integer
    /// </summary>
    private long GenerateOrderCode(string orderCode)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = new Random().Next(1000, 9999);
        return timestamp * 10000 + random;
    }

    /// <summary>
    /// Create PayOS client from configuration
    /// </summary>
    private PayOSClient CreatePayOSClient(PayOSSetting configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return new PayOSClient(
            configuration.ClientId, 
            configuration.ApiKey, 
            configuration.ChecksumKey);
    }

    /// <summary>
    /// Validate PayOS configuration has all required fields
    /// </summary>
    private bool IsValidConfig(PayOSSetting setting)
    {
        return setting != null &&
               !string.IsNullOrWhiteSpace(setting.ClientId) &&
               !string.IsNullOrWhiteSpace(setting.ApiKey) &&
               !string.IsNullOrWhiteSpace(setting.ChecksumKey);
    }
    
    public Task<PaymentRefundResult> RefundPaymentAsync(string transactionId, decimal amount, string reason, string configuration,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    // public async Task<PaymentRefundResult> RefundPaymentAsync(
    //     string transactionId,
    //     decimal amount,
    //     string reason,
    //     string configuration,
    //     CancellationToken cancellationToken = default)
    // {
    //     try
    //     {
    //         var config = JsonSerializer.Deserialize<PayOSConfig>(configuration);
    //         if (config == null || !IsValidConfig(config))
    //         {
    //             return new PaymentRefundResult
    //             {
    //                 Success = false,
    //                 ErrorMessage = "Invalid PayOS configuration"
    //             };
    //         }
    //
    //         var payOSClient = CreatePayOSClient(config);
    //
    //         _logger.Information(
    //             "Initiating PayOS refund - TransactionId: {TransactionId}, Amount: {Amount}",
    //             transactionId, amount);
    //
    //         // Call PayOS Cancel/Refund API
    //         var cancelResult = await payOSClient.CancelPaymentLink(
    //             paymentLinkId: transactionId,
    //             cancellationReason: reason
    //         );
    //
    //         if (cancelResult != null)
    //         {
    //             _logger.Information(
    //                 "PayOS refund successful - TransactionId: {TransactionId}",
    //                 transactionId);
    //
    //             return new PaymentRefundResult
    //             {
    //                 Success = true,
    //                 RefundTransactionId = transactionId,
    //                 RefundedAmount = amount,
    //                 RefundFee = 0,
    //                 ActualRefundAmount = amount,
    //                 RefundedAt = DateTime.UtcNow
    //             };
    //         }
    //
    //         return new PaymentRefundResult
    //         {
    //             Success = false,
    //             ErrorMessage = "PayOS refund failed"
    //         };
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.Error(ex, "Error refunding PayOS payment: {TransactionId}", transactionId);
    //         return new PaymentRefundResult
    //         {
    //             Success = false,
    //             ErrorMessage = ex.Message
    //         };
    //     }
    // }
}
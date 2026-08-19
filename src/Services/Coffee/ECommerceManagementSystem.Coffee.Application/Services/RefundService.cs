using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Settings;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class RefundService : IRefundService
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly ILogger _logger;
    private readonly RefundSettings _refundSettings;

    public RefundService(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        IPaymentGatewayFactory paymentGatewayFactory,
        ILogger logger,
        IOptions<RefundSettings> refundSettings)
    {
        _unitOfWork = unitOfWork;
        _paymentGatewayFactory = paymentGatewayFactory;
        _logger = logger;
        _refundSettings = refundSettings.Value;
    }

    public async Task<RefundRequests> CreateRefundRequestAsync(
        Orders order,
        Guid requestedBy,
        ERole requestedByRole,
        string? cancelReason,
        CancellationToken cancellationToken = default)
    {
        _logger.Information(
            "Creating refund request for order {OrderCode}, RequestedBy: {UserId}, Role: {Role}",
            order.Code, requestedBy, requestedByRole);

        // Get payment info
        var payment = await _unitOfWork.GetRepository<Payments>()
            .SingleOrDefaultAsync(
                predicate: x => x.OrderId == order.Id
            );

        if (payment == null)
        {
            throw new Exception("Payment not found for order!");
        }

        // Determine refund mode and method
        var (mode, method) = DetermineRefundModeAndMethod(payment);

        var refundRequest = new RefundRequests
        {
            Id = Guid.CreateVersion7(),
            OrderId = order.Id,
            RefundAmount = order.TotalAmount,
            Status = ERefundStatus.Pending,
            Method = method,
            Mode = mode,
            
            // Manual fields
            DueDate = mode == ERefundMode.Manual 
                ? DateTime.UtcNow.AddDays(_refundSettings.ManualRefundSLA) 
                : null,
            
            // Automatic fields
            PaymentGatewayTransactionId = payment.TransactionId,
            
            // Tracking
            RequestedBy = requestedBy,
            RequestedByRole = requestedByRole,
            RequestedAt = DateTime.UtcNow,
            RetryCount = 0,
            RemindersSent = 0,
            
            CreatedDate = DateTime.UtcNow
        };

        await _unitOfWork.GetRepository<RefundRequests>().InsertAsync(refundRequest);
        await _unitOfWork.CommitAsync();

        _logger.Information(
            "Refund request created: {RefundId}, Mode: {Mode}, Method: {Method}",
            refundRequest.Id, mode, method);

        // If automatic mode is enabled, process immediately in background
        if (mode == ERefundMode.Automatic && _refundSettings.AutomaticRefundEnabled)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                await ProcessRefundAsync(refundRequest, cancellationToken);
            }, cancellationToken);
        }

        return refundRequest;
    }

    public async Task<RefundResult> ProcessRefundAsync(
        RefundRequests refundRequest,
        CancellationToken cancellationToken = default)
    {
        if (refundRequest.Mode == ERefundMode.Manual)
        {
            return new RefundResult
            {
                Success = false,
                Status = ERefundStatus.Pending,
                Message = "Manual refund requires admin processing"
            };
        }

        return await ProcessAutomaticRefundAsync(refundRequest, cancellationToken);
    }

    private async Task<RefundResult> ProcessAutomaticRefundAsync(
        RefundRequests refundRequest,
        CancellationToken cancellationToken)
    {
        _logger.Information(
            "Processing automatic refund: {RefundId}, Method: {Method}",
            refundRequest.Id, refundRequest.Method);

        try
        {
            // Update status to Processing
            refundRequest.Status = ERefundStatus.Processing;
            refundRequest.ProcessedAt = DateTime.UtcNow;
            refundRequest.ProcessedBy = Guid.Empty; // System
            _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
            await _unitOfWork.CommitAsync();

            // Get order with payment method config
            var payment = await _unitOfWork.GetRepository<Payments>()
                .SingleOrDefaultAsync(predicate: x => x.OrderId == refundRequest.OrderId);

            var brandPaymentMethod = await _unitOfWork.GetRepository<BrandPaymentMethods>()
                .SingleOrDefaultAsync(
                    predicate: x => x.PaymentMethodId == payment.PaymentMethodId,
                    include: i => i.Include(x => x.PaymentMethods)
                );

            if (brandPaymentMethod == null)
            {
                throw new Exception("BrandPaymentMethod not found!");
            }

            // Resolve payment gateway
            var gateway = _paymentGatewayFactory.GetGatewayByBrandPaymentMethod(brandPaymentMethod);

            // Call refund API
            var gatewayResult = await gateway.RefundPaymentAsync(
                refundRequest.PaymentGatewayTransactionId!,
                refundRequest.RefundAmount,
                $"Order cancelled",
                brandPaymentMethod.Configuration ?? "{}",
                cancellationToken
            );

            if (gatewayResult.Success)
            {
                // Refund successful
                refundRequest.Status = ERefundStatus.Completed;
                refundRequest.RefundTransactionId = gatewayResult.RefundTransactionId;
                refundRequest.GatewayResponse = JsonSerializer.Serialize(gatewayResult);
                refundRequest.GatewayRefundFee = gatewayResult.RefundFee;
                refundRequest.ActualRefundAmount = gatewayResult.ActualRefundAmount;
                refundRequest.CompletedAt = DateTime.UtcNow;
                refundRequest.LastModifiedDate = DateTime.UtcNow;

                _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
                await _unitOfWork.CommitAsync();

                _logger.Information(
                    "Automatic refund completed: {RefundId}, TransactionId: {TransactionId}",
                    refundRequest.Id, gatewayResult.RefundTransactionId);

                return new RefundResult
                {
                    Success = true,
                    Status = ERefundStatus.Completed,
                    Message = "Refund completed successfully",
                    RefundTransactionId = gatewayResult.RefundTransactionId,
                    ActualRefundAmount = gatewayResult.ActualRefundAmount,
                    CompletedAt = refundRequest.CompletedAt
                };
            }
            else
            {
                // Refund failed
                refundRequest.Status = ERefundStatus.Failed;
                refundRequest.LastErrorMessage = gatewayResult.ErrorMessage;
                refundRequest.GatewayResponse = JsonSerializer.Serialize(gatewayResult);
                refundRequest.RetryCount++;
                refundRequest.LastModifiedDate = DateTime.UtcNow;

                _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
                await _unitOfWork.CommitAsync();

                _logger.Warning(
                    "Automatic refund failed: {RefundId}, Error: {Error}, RetryCount: {RetryCount}",
                    refundRequest.Id, gatewayResult.ErrorMessage, refundRequest.RetryCount);

                return new RefundResult
                {
                    Success = false,
                    Status = ERefundStatus.Failed,
                    ErrorMessage = gatewayResult.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing automatic refund: {RefundId}", refundRequest.Id);

            refundRequest.Status = ERefundStatus.Failed;
            refundRequest.LastErrorMessage = ex.Message;
            refundRequest.RetryCount++;
            refundRequest.LastModifiedDate = DateTime.UtcNow;

            _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
            await _unitOfWork.CommitAsync();

            return new RefundResult
            {
                Success = false,
                Status = ERefundStatus.Failed,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<RefundResult> CompleteManualRefundAsync(
        Guid refundRequestId,
        Guid adminId,
        string transferReference,
        string? transferProofImagePath,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var refundRequest = await _unitOfWork.GetRepository<RefundRequests>()
            .SingleOrDefaultAsync(predicate: x => x.Id == refundRequestId);

        if (refundRequest == null)
        {
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "Refund request not found!"
            };
        }

        if (refundRequest.Mode != ERefundMode.Manual)
        {
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "Only manual refunds can be marked as completed!"
            };
        }

        refundRequest.Status = _refundSettings.RequireCustomerConfirmation
            ? ERefundStatus.WaitingCustomerConfirmation
            : ERefundStatus.Completed;
        refundRequest.TransferReference = transferReference;
        refundRequest.TransferProofImagePath = transferProofImagePath;
        refundRequest.AdminNote = adminNote;
        refundRequest.ProcessedBy = adminId;
        refundRequest.ProcessedAt = DateTime.UtcNow;
        refundRequest.ActualRefundAmount = refundRequest.RefundAmount;

        if (!_refundSettings.RequireCustomerConfirmation)
        {
            refundRequest.CompletedAt = DateTime.UtcNow;
        }

        refundRequest.LastModifiedDate = DateTime.UtcNow;

        _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
        await _unitOfWork.CommitAsync();

        _logger.Information(
            "Manual refund marked as completed: {RefundId}, ProcessedBy: {AdminId}",
            refundRequestId, adminId);

        return new RefundResult
        {
            Success = true,
            Status = refundRequest.Status,
            Message = _refundSettings.RequireCustomerConfirmation
                ? "Waiting for customer confirmation"
                : "Refund completed",
            CompletedAt = refundRequest.CompletedAt
        };
    }

    public async Task<bool> CustomerConfirmReceivedAsync(
        Guid refundRequestId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var refundRequest = await _unitOfWork.GetRepository<RefundRequests>()
            .SingleOrDefaultAsync(
                predicate: x => x.Id == refundRequestId,
                include: i => i.Include(x => x.Order)
            );

        if (refundRequest == null)
        {
            return false;
        }

        if (refundRequest.Order.CustomerId != customerId)
        {
            _logger.Warning(
                "Customer {CustomerId} attempted to confirm refund {RefundId} for order owned by {OwnerId}",
                customerId, refundRequestId, refundRequest.Order.CustomerId);
            return false;
        }

        refundRequest.CustomerConfirmedReceived = true;
        refundRequest.CustomerConfirmedAt = DateTime.UtcNow;
        refundRequest.Status = ERefundStatus.Completed;
        refundRequest.CompletedAt = DateTime.UtcNow;
        refundRequest.LastModifiedDate = DateTime.UtcNow;

        _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
        await _unitOfWork.CommitAsync();

        _logger.Information(
            "Customer confirmed refund received: {RefundId}, CustomerId: {CustomerId}",
            refundRequestId, customerId);

        return true;
    }

    public async Task<RefundResult> RetryAutomaticRefundAsync(
        Guid refundRequestId,
        CancellationToken cancellationToken = default)
    {
        var refundRequest = await _unitOfWork.GetRepository<RefundRequests>()
            .SingleOrDefaultAsync(predicate: x => x.Id == refundRequestId);

        if (refundRequest == null)
        {
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "Refund request not found!"
            };
        }

        if (refundRequest.Mode != ERefundMode.Automatic)
        {
            return new RefundResult
            {
                Success = false,
                ErrorMessage = "Only automatic refunds can be retried!"
            };
        }

        if (refundRequest.RetryCount >= _refundSettings.AutomaticRefundRetryAttempts)
        {
            return new RefundResult
            {
                Success = false,
                ErrorMessage = $"Maximum retry attempts ({_refundSettings.AutomaticRefundRetryAttempts}) exceeded!"
            };
        }

        refundRequest.Status = ERefundStatus.Pending;
        _unitOfWork.GetRepository<RefundRequests>().UpdateAsync(refundRequest);
        await _unitOfWork.CommitAsync();

        return await ProcessAutomaticRefundAsync(refundRequest, cancellationToken);
    }

    /// <summary>
    /// Determine refund mode and method based on payment method and configuration
    /// </summary>
    private (ERefundMode mode, ERefundMethod method) DetermineRefundModeAndMethod(Payments payment)
    {
        var paymentCode = payment.PaymentMethodCodeSnapshot;

        // If automatic refund is disabled globally, use manual
        if (!_refundSettings.AutomaticRefundEnabled)
        {
            return (ERefundMode.Manual, ERefundMethod.BankTransfer);
        }

        // Determine based on payment method
        return paymentCode.ToUpperInvariant() switch
        {
            "PAYOS" => (ERefundMode.Automatic, ERefundMethod.PayOSRefund),
            "VNPAY" => (ERefundMode.Automatic, ERefundMethod.VNPayRefund),
            "MOMO" => (ERefundMode.Automatic, ERefundMethod.MoMoRefund),
            "PAYINCASH" => (ERefundMode.Manual, ERefundMethod.Cash),
            _ => (ERefundMode.Manual, ERefundMethod.BankTransfer)
        };
    }
}
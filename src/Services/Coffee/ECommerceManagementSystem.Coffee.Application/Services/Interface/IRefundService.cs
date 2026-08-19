using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface IRefundService
{
    /// <summary>
    /// Create refund request when order is cancelled
    /// Automatically determines mode based on configuration and payment method
    /// </summary>
    Task<RefundRequests> CreateRefundRequestAsync(
        Orders order,
        Guid requestedBy,
        ERole requestedByRole,
        string? cancelReason,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Process refund (manual or automatic based on mode)
    /// </summary>
    Task<RefundResult> ProcessRefundAsync(
        RefundRequests refundRequest,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Admin manually marks refund as completed (manual mode only)
    /// </summary>
    Task<RefundResult> CompleteManualRefundAsync(
        Guid refundRequestId,
        Guid adminId,
        string transferReference,
        string? transferProofImagePath,
        string? adminNote,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Customer confirms received money (manual mode only)
    /// </summary>
    Task<bool> CustomerConfirmReceivedAsync(
        Guid refundRequestId,
        Guid customerId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retry failed automatic refund
    /// </summary>
    Task<RefundResult> RetryAutomaticRefundAsync(
        Guid refundRequestId,
        CancellationToken cancellationToken = default);
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class RefundRequests : EntityAuditBase<Guid>
{
    public Guid OrderId { get; set; }
    public decimal RefundAmount { get; set; }
    public ERefundStatus Status { get; set; }
    public ERefundMethod Method { get; set; }
    public ERefundMode Mode { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountName { get; set; }
    public string? BankName { get; set; }
    public string? TransferProofImagePath { get; set; }
    public string? TransferProofImageUrl { get; set; }
    public string? TransferReference { get; set; }
    public string? PaymentGatewayTransactionId { get; set; }
    public string? RefundTransactionId { get; set; }
    public string? GatewayResponse { get; set; }
    public decimal? GatewayRefundFee { get; set; }
    public decimal? ActualRefundAmount { get; set; }
    public Guid RequestedBy { get; set; }
    public ERole RequestedByRole { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid? ProcessedBy { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RemindersSent { get; set; }
    public bool? CustomerConfirmedReceived { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }
    public string? AdminNote { get; set; }
    public string? RejectionReason { get; set; }
    public int RetryCount { get; set; }
    public string? LastErrorMessage { get; set; }
    public virtual Orders Order { get; set; } = null!;
}
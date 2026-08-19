using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

public class RefundResult
{
    public bool Success { get; set; }
    public ERefundStatus Status { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RefundTransactionId { get; set; }
    public decimal? ActualRefundAmount { get; set; }
    public DateTime? CompletedAt { get; set; }
}
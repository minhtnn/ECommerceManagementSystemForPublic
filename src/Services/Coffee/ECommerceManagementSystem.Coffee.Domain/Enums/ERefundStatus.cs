namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum ERefundStatus
{
    /// <summary>
    /// Waiting for processing (Manual: waiting admin, Automatic: queued)
    /// </summary>
    Pending,
    
    /// <summary>
    /// Being processed (Manual: admin working, Automatic: API calling)
    /// </summary>
    Processing,
    
    /// <summary>
    /// Successfully refunded
    /// </summary>
    Completed,
    
    /// <summary>
    /// Refund failed
    /// </summary>
    Failed,
    
    /// <summary>
    /// Admin rejected the refund request
    /// </summary>
    Rejected,
    
    /// <summary>
    /// Waiting for customer confirmation (manual mode only)
    /// </summary>
    WaitingCustomerConfirmation
}
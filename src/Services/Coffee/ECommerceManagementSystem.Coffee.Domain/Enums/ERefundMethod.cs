namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum ERefundMethod
{
    /// <summary>
    /// Manual bank transfer by admin
    /// </summary>
    BankTransfer,
    
    /// <summary>
    /// Automatic refund via PayOS API
    /// </summary>
    PayOSRefund,
    
    /// <summary>
    /// Automatic refund via VNPay API
    /// </summary>
    VNPayRefund,
    
    /// <summary>
    /// Automatic refund via MoMo API
    /// </summary>
    MoMoRefund,
    
    /// <summary>
    /// Cash on delivery - no online refund needed
    /// </summary>
    Cash,
    
    /// <summary>
    /// Refund to store credit/wallet (future feature)
    /// </summary>
    StoreCredit
}
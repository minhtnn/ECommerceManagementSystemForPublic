namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum ERefundMode
{
    /// <summary>
    /// Admin manually transfers money and uploads proof
    /// </summary>
    Manual,
    
    /// <summary>
    /// System automatically calls payment gateway API
    /// </summary>
    Automatic
}
namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum ERuleConditionType
{
    // Nhóm Cart/Order level
    CartSubtotal, // subtotal >= value
    CartContainsProduct, // cart phải có product(s) cụ thể 
    CartContainsCategory, // cart phải có sản phẩm thuộc category

    // Nhóm Quantity
    MinQuantityOfProduct, // số lượng của product cụ thể >= X (dùng cho BuyXGetY)
    MinQuantityInCategory, // tổng số lượng của products trong category >= X (dùng cho QuantityTier)
    TotalCartQuantity, // tổng số lượng toàn cart >= X
    
    FirstOrder,
}
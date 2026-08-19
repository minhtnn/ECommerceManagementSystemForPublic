namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum EPromotionType
{
    OrderDiscount,     // Type 1: subtotal >= min + optional required products
    LineItemDiscount,   // Type 2: discount trên sản phẩm/category cụ thể
    BuyXGetY,           // Type 3: mua X tặng Y
    // QuantityTier,       // Type 4: nhiều tầng số lượng
    FreeGift,           // Type 5: subtotal >= threshold tặng quà
    FreeShipping,       // Type 6: miễn phí vận chuyển (exclusive action)
}
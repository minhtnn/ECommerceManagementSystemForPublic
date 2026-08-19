namespace ECommerceManagementSystem.Coffee.Domain.Enums;

public enum EActionTargetRole
{
    DiscountTarget, // sản phẩm được giảm giá (Type 2: LineItemDiscount)
    BuyProduct, // sản phẩm phải mua (Type 3: BuyXGetY - phần Buy)
    GetProduct, // sản phẩm được tặng (Type 3: BuyXGetY - phần Get)
    GiftProduct, // quà tặng cố định (Type 5: FreeGift)
}
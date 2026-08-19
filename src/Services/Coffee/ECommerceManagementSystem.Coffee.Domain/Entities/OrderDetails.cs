using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class OrderDetails : EntityAuditBase<Guid>
{
    public Guid OrderId { get; set; }
    public required Guid ProductId { get; set; }
    public string? ProductNameSnapshot { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal TotalPriceSnapshot { get; set; }
    public bool IsGiftItem { get; set; } = false;
    public Guid? GiftFromPromotionId { get; set; }

    public virtual Orders? Order { get; set; }
    public virtual Products? Product { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class AppliedOrderPromotions : EntityAuditBase<Guid>
{
    public required Guid PromotionRuleId { get; set; }
    public required Guid OrderId { get; set; }
    public string? PromotionRuleNameSnapshot { get; set; }
    public decimal DiscountAmountApplied  { get; set; }
    public EStackingSlot StackingSlot {get; set;}
    public int ApplyOrder {get; set;}
    
    public virtual PromotionRules? PromotionRule { get; set; }
    public virtual Orders? Order { get; set; }
}
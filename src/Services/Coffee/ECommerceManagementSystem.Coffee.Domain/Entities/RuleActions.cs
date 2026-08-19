using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class RuleActions : EntityAuditBase<Guid>
{
    public required Guid PromotionRuleId { get; set; }
    public ERuleActionType ActionType  { get; set; }
    public string? Value { get; set; }
    public decimal? MaxDiscountAmountForPercentage  { get; set; }
    
    public virtual PromotionRules? PromotionRule { get; set; }
    public virtual List<RuleActionTargets>? RuleActionTargets { get; set; }
}
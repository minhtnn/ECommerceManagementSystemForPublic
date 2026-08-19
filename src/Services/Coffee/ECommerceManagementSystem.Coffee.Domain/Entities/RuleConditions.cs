using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class RuleConditions : EntityAuditBase<Guid>
{
    public required Guid PromotionRuleId { get; set; }
    public ERuleConditionType ConditionType  { get; set; }
    public ERuleConditionOperator Operator { get; set; }
    public string? Value { get; set; }

    public virtual PromotionRules? PromotionRule { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;

public class GetPromotionRuleByIdResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ShortDescription  { get; set; }
    public string? Description  { get; set; }
    public EPromotionStatus  Status { get; set; }
    public EPromotionType PromotionType { get; set; }
    public int Priority { get; set; }
    public decimal GlobalDiscountCap {get; set;}
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }

    public List<GetBrandPromotionRuleCondition> RuleConditions { get; set; } = new();
    public List<GetBrandPromotionRuleAction> RuleActions { get; set; } = new();
}

public class GetBrandPromotionRuleCondition
{
    public Guid Id { get; set; }
    public required Guid PromotionRuleId { get; set; }
    public ERuleConditionType ConditionType  { get; set; }
    public ERuleConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

public class GetBrandPromotionRuleAction
{
    public Guid Id { get; set; }
    public required Guid PromotionRuleId { get; set; }
    public ERuleActionType ActionType  { get; set; }
    public string? Value { get; set; }
    public decimal MaxDiscountAmountForPercentage  { get; set; }
    public List<GetBrandPromotionRuleActionTargets> RuleActionTargets { get; set; }
}

public class GetBrandPromotionRuleActionTargets
{
    public Guid Id { get; set; }
    public Guid RuleActionId { get; set; }
    public EActionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public int Quantity { get; set; }
    public EActionTargetRole Role { get; set; }
}
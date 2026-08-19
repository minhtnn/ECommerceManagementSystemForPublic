using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.CreateBrandPromotionRule;

public class CreateBrandPromotionRuleCommand : IRequest<ApiResponse>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public EPromotionType PromotionType { get; set; }
    public decimal GlobalDiscountCap { get; set; }
    public int Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? TimeZone { get; set; }
    public List<CreateBrandRuleCondition>? RuleConditions { get; set; }
    public List<CreateBrandRuleAction>? RuleActions { get; set; }
}

public class CreateBrandRuleCondition
{
    public ERuleConditionType ConditionType { get; set; }
    public ERuleConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

public class CreateBrandRuleAction
{
    public ERuleActionType ActionType { get; set; }
    public string? Value { get; set; }
    public decimal MaxDiscountAmountForPercentage { get; set; }
    public List<CreateBrandRuleActionTarget>? RuleActionTargets { get; set; }
}

public class CreateBrandRuleActionTarget
{
    // public ERuleActionType ActionType { get; set; }
    public EActionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public int Quantity { get; set; }
    public EActionTargetRole Role { get; set; }
}
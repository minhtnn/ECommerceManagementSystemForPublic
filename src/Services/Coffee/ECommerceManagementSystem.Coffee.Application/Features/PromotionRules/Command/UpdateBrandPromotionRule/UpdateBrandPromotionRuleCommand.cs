using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Command.UpdateBrandPromotionRule;

public class UpdateBrandPromotionRuleCommand : IRequest<ApiResponse>
{
    public required Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public EPromotionType? PromotionType { get; set; }
    public decimal? GlobalDiscountCap { get; set; }
    public int? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? TimeZone { get; set; }
    
    public EPromotionStatus? Status { get; set; }
    /// <summary>
    /// Khi truyền lên sẽ Replace All toàn bộ conditions hiện tại.
    /// Null = không thay đổi conditions (chỉ hợp lệ khi chưa bắt đầu).
    /// </summary>
    public List<UpdateBrandRuleCondition>? RuleConditions { get; set; }

    /// <summary>
    /// Khi truyền lên sẽ Replace All toàn bộ actions hiện tại.
    /// Null = không thay đổi actions (chỉ hợp lệ khi chưa bắt đầu).
    /// </summary>
    public List<UpdateBrandRuleAction>? RuleActions { get; set; }
}

public class UpdateBrandRuleCondition
{
    public ERuleConditionType ConditionType { get; set; }
    public ERuleConditionOperator Operator { get; set; }
    public string? Value { get; set; }
}

public class UpdateBrandRuleAction
{
    public ERuleActionType ActionType { get; set; }
    public string? Value { get; set; }
    public decimal? MaxDiscountAmountForPercentage { get; set; }
    public List<UpdateBrandRuleActionTarget>? RuleActionTargets { get; set; }
}

public class UpdateBrandRuleActionTarget
{
    public EActionTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public int Quantity { get; set; }
    public EActionTargetRole Role { get; set; }
}
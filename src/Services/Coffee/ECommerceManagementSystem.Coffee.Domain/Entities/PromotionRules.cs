using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class PromotionRules : EntityAuditBase<Guid>
{
    public required Guid BrandId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ShortDescription  { get; set; }
    public string? Description  { get; set; }
    public EPromotionType PromotionType {get; set;}
    public decimal? GlobalDiscountCap {get; set;}
    public int Priority {get; set;}
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EPromotionStatus  Status { get; set; }
    
    public virtual Brands?   Brand { get; set; }
    public virtual List<RuleConditions> RuleConditions { get; set; } = new List<RuleConditions>();
    public virtual List<RuleActions> RuleActions { get; set; } = new List<RuleActions>();
    public virtual List<AppliedOrderPromotions> AppliedOrderPromotions { get; set; } =
        new List<AppliedOrderPromotions>();
}
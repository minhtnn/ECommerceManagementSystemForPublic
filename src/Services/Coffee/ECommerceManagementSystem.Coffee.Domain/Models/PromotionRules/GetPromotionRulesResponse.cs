using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;

public class GetPromotionRulesResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ShortDescription  { get; set; }
    public int Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public EPromotionStatus  Status { get; set; }
}
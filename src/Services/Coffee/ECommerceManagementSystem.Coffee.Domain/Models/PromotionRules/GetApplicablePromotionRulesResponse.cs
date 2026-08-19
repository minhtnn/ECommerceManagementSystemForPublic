using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.PromotionRules;

public class GetApplicablePromotionRulesResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? ShortDescription  { get; set; }
    public string? Description  { get; set; }

}
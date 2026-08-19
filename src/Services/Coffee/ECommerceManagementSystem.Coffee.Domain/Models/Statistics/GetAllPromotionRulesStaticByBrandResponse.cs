namespace ECommerceManagementSystem.Coffee.Domain.Models.Statistics;

public class GetAllPromotionRulesStaticByBrandResponse
{
    public required Guid PromotionRuleId { get; set; }
    public required string PromotionNameSnapshot { get; set; }
    public required DateOnly StatDate { get; set; }
    public decimal TotalDiscountIssued { get; set; }
    public int TotalOrdersUsed { get; set; }
    public decimal TotalRevenueWithPromo { get; set; }
}
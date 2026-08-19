using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPublicPromotionRule;

public class GetBrandPublicPromotionRuleQuery : IRequest<ApiResponse>
{
    public required string BrandCode {get;set;}
    public string? Code { get; set; }
    public string? Name { get; set; }
}
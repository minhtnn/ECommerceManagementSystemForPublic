using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.PromotionRules.Query.GetBrandPromotionRuleById;

public class GetBrandPromotionRuleByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string TimeZone {get;set;}
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrandDetails;

public class GetBrandDetailsQuery : IRequest<ApiResponse>
{
    public required string TimeZone {get;set;}
}
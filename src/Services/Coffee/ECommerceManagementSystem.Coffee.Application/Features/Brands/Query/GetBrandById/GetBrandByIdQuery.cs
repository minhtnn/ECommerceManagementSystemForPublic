using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Query.GetBrandById;

public class GetBrandByIdQuery : IRequest<ApiResponse>
{
    public Guid BrandId { get; set; }
    public required string TimeZone {get;set;}
}
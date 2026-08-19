using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetMenuByBrand;

public class GetMenuByBrandQuery : IRequest<ApiResponse>
{
    public Guid? CategoryId { get; set; } = null;
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Menus.Query.GetPublicMenuByBrand;

public class GetPublicMenuByBrandQuery: IRequest<ApiResponse>
{
    public required string BrandCode { get; set; } = string.Empty;
    public int Page { get; set; }
    public int Size { get; set; }
    public Guid? CategoryId { get; set; }
    public string? ProductsSortBy { get; set; }
    public bool ProductsIsAsc { get; set; }
    public string? ProductName { get; set; }
}
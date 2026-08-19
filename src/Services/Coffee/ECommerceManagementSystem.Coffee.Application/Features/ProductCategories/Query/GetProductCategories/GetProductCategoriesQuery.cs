using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.ProductCategories.Query.GetProductCategories;

public class GetProductCategoriesQuery : IRequest<ApiResponse>
{
    public int Page { get; set; }
    public int Size { get; set; }
    public string? SortBy { get; set; }
    public bool IsAsc { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public bool? IsLeafOnly { get; set; }
    public ECategoryStatus? Status { get; set; }
}
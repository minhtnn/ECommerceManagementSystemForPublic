using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPosts;

public class GetBrandPublicPostsQuery : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool IsAsc { get; set; } = false;
    public required string TimeZone {get;set;}
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPublicPostById;

public class GetBrandPublicPostByIdQuery : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public required string TimeZone { get; set; }
}
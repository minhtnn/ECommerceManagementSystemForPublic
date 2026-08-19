using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Query.GetBrandPostById;

public class GetBrandPostByIdQuery : IRequest<ApiResponse>
{
    public Guid Id { get; set; }
    public required string TimeZone { get; set; }
}
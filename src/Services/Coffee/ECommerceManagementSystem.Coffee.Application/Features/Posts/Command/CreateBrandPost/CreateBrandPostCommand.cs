using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Posts.Command.CreateBrandPost;

public class CreateBrandPostCommand : IRequest<ApiResponse>
{
    public required string Code { get; set; }
    public required string Title { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? Excerpt  { get; set; }
    public IFormFile? Image { get; set; }
    public IFormFileCollection? InlineImages { get; set; }
}
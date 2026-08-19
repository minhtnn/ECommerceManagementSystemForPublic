using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Posts;

public class GetBrandPostsResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Title { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public EPostStatus Status { get; set; }
}
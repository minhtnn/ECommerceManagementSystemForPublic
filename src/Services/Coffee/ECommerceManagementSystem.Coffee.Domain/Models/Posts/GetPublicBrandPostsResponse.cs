namespace ECommerceManagementSystem.Coffee.Domain.Models.Posts;

public class GetPublicBrandPostsResponse
{
    public Guid Id { get; set; }
    // public string Code { get; set; }
    public string Title { get; set; }
    public string? Author { get; set; }       
    public string? Excerpt { get; set; }      
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime? PublishedAt { get; set; } 
}
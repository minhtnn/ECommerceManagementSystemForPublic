using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;
using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class Posts : EntityAuditBase<Guid>
{
    public required Guid BrandId { get; set; }
    public required string Code { get; set; }
    public required string Title { get; set; }
    public string? Author { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? Excerpt  { get; set; }
    public string? FeaturedImage { get; set; }
    public EPostStatus Status { get; set; }
    public DateTime? PublishedAt { get; set; }
    
    public virtual Brands? Brand { get; set; }
}
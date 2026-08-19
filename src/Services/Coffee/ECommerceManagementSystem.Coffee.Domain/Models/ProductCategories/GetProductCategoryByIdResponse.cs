using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;

public class GetProductCategoryByIdResponse
{
    public Guid Id { get; set; }
    public string? ParentProductCategoryName { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public int Level  { get; set; }
    public bool IsLeafOnly { get; set; }
    public bool IsDeletable { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public ECategoryStatus Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}
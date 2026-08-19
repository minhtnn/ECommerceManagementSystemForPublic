using ECommerceManagementSystem.Coffee.Domain.Enums;

namespace ECommerceManagementSystem.Coffee.Domain.Models.ProductCategories;

public class GetProductCategoriesResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int Level { get; set; }
    public bool IsLeafOnly { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public ECategoryStatus Status { get; set; }
}
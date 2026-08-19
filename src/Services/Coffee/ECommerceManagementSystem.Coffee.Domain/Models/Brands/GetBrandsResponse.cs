using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Brands;

public class GetBrandsResponse
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Email  { get; set; }
    public string? LogoPath  { get; set; }
    public string? LogoUrl { get; set; }
    public EBrandStatus Status { get; set; }
}
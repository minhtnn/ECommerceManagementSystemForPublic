using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Brands;

public class GetBrandDetailsResponse
{
    public Guid Id {get; set;}
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Fullname { get; set; }
    public string? Slogan { get; set; }
    public string? Email  { get; set; }
    public string? Address   { get; set; }
    public string? PhoneNumber { get; set; }
    public string? LogoPath  { get; set; }
    public string? LogoUrl  { get; set; }
    public EBrandStatus Status { get; set; }
    public string? Configuration { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastModifiedDate { get; set; }
}
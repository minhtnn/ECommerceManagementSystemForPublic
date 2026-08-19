using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.UpdateBrand;

public class UpdateBrandCommand : IRequest<ApiResponse>
{
    public required Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Fullname { get; set; }
    public string? Slogan { get; set; }
    public required string Email  { get; set; }
    public required string Address   { get; set; }
    public string? PhoneNumber { get; set; }
    public IFormFile? Logo  { get; set; }
    public EBrandStatus Status { get; set; }
    public string? Configuration { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Brands.Command.CreateBrand;

public class CreateBrandCommand: IRequest<ApiResponse>
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public required string Email  { get; set; }
    public required string Address  { get; set; }
    public string? PhoneNumber { get; set; }
    public IFormFile? Logo  { get; set; }
    public string? Configuration { get; set; }
    public required string Username{get;set;}
    public required string PasswordString{get;set;}
}
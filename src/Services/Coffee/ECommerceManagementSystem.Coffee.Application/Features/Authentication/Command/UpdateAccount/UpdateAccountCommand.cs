using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.UpdateAccount;

public class UpdateAccountCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public IFormFile? Image { get; set; }
    public string? Name { get; set; }
    public string? FullName { get; set; }
    // public string? Username { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}
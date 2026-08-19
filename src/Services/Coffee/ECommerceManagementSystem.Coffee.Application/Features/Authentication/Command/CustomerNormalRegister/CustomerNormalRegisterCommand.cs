using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerNormalRegister;

public class CustomerNormalRegisterCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public IFormFile? Avatar { get; set; }
    public string? PhoneNumber {get; set;}
    public required string Email { get; set; }
    public required string Username { get; set; }
    public required string FullName { get; set; }
    public required string PasswordString { get; set; }
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Login;

public class LoginCommand : IRequest<ApiResponse>
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public required string Password { get; set; }
    public required string BrandCode { get; set; }
    public required string TimeZone { get; set; }
}
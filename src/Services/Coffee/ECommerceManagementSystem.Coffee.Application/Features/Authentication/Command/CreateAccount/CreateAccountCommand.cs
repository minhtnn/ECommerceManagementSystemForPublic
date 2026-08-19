using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CreateAccount;

public class CreateAccountCommand : IRequest<ApiResponse>
{
    public required string Email { get; set; }
    public string? Username { get; set; }
    public required string PasswordString { get; set; }
}
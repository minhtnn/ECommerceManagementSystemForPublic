using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ValidateResetToken;

public class ValidateResetTokenCommand : IRequest<ApiResponse>
{
    public required string Email { get; set; }
    public required string Token { get; set; }
    public required string BrandCode { get; set; }  
}
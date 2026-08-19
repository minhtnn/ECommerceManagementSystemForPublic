using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.ResendOTP.Email;

public class ResendEmailOTPCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public required string Email { get; set; }
}
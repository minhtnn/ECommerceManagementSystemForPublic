using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.VerifyCustomerEmail;

public class VerifyCustomerEmailCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public required string Email { get; set; }
    public required string OtpCode { get; set; }
    public required string TimeZone { get; set; }
}
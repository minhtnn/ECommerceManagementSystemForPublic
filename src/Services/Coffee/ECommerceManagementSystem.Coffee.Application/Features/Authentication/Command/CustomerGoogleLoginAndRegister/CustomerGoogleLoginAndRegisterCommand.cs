using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.CustomerGoogleLoginAndRegister;

public class CustomerGoogleLoginAndRegisterCommand : IRequest<ApiResponse>
{
    public required string BrandCode { get; set; }
    public required string IdToken { get; set; }
    public required string TimeZone { get; set; }
}
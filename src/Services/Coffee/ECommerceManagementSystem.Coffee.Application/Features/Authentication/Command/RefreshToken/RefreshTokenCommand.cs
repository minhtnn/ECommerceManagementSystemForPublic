using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.RefreshToken;

public class RefreshTokenCommand : IRequest<ApiResponse>
{
    public required string TimeZone {get;set;}
}
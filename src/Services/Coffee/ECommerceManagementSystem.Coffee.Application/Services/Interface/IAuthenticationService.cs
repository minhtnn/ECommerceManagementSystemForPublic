using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface IAuthenticationService
{
    (DateTime accessTokenExpiry, DateTime refreshTokenExpiry) GetJwtExpireConfiguration();
    string GenerateAccessTokenAsync(Accounts account);
    string GenerateRefreshTokensAsync(Accounts account);

}
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IClaimService _claimService;

    public LogoutCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork, ILogger logger,
        IHttpContextAccessor httpContextAccessor, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new BadHttpRequestException("HTTP context not available");
        }

        try
        {
            // BƯỚC 1: Lấy refresh token từ cookie
            var refreshTokenString = httpContext.Request.Cookies["refreshToken"];

            if (!string.IsNullOrWhiteSpace(refreshTokenString))
            {
                // BƯỚC 2: Tìm và revoke refresh token trong DB
                var storedToken = await _unitOfWork.GetRepository<RefreshTokens>()
                    .SingleOrDefaultAsync(
                        predicate: x => x.Token == refreshTokenString);

                if (storedToken != null && !storedToken.IsRevoked)
                {
                    // Revoke token
                    storedToken.IsRevoked = true;
                    storedToken.RevokedDate = DateTime.UtcNow;
                    storedToken.LastModifiedDate = DateTime.UtcNow;
                    storedToken.RevokedByIp = httpContext.Connection.RemoteIpAddress?.ToString();

                    _unitOfWork.GetRepository<RefreshTokens>()
                        .UpdateAsync(storedToken);

                    await _unitOfWork.CommitAsync();

                    _logger.Information(
                        "Refresh token revoked for account {AccountId} from IP {IP}",
                        storedToken.AccountId,
                        storedToken.RevokedByIp);
                }
            }

            // BƯỚC 3: Lấy accountId từ access token (nếu có)
            var accountId = _claimService.GetCurrentAccountId();

            if (accountId != Guid.Empty)
            {
                _logger.Information("User {AccountId} logged out successfully", accountId);
            }

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Đăng xuất thành công"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during logout");

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = "Đăng xuất thành công"
            };
        }
    }
}
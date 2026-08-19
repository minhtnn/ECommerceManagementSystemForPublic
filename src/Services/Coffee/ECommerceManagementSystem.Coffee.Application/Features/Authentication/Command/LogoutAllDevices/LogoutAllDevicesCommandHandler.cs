using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Mediator;

namespace ECommerceManagementSystem.Coffee.Application.Features.Authentication.Command.LogoutAllDevices;

public class LogoutAllDevicesCommandHandler : IRequestHandler<LogoutAllDevicesCommand, ApiResponse>
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;
    private readonly IClaimService _claimService;

    public LogoutAllDevicesCommandHandler(IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger, IClaimService claimService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _claimService = claimService;
    }

    public async ValueTask<ApiResponse> Handle(LogoutAllDevicesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // BƯỚC 1: Lấy accountId từ access token
            var accountId = _claimService.GetCurrentAccountId();

            if (accountId == Guid.Empty)
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng");
            }

            // BƯỚC 2: Lấy tất cả refresh tokens active của user
            var activeTokens = await _unitOfWork.GetRepository<RefreshTokens>()
                .GetListAsync(
                    predicate: x => x.AccountId == accountId && !x.IsRevoked);

            if (!activeTokens.Any())
            {
                return new ApiResponse
                {
                    Status = StatusCodes.Status200OK,
                    Message = "Không có phiên đăng nhập nào để đăng xuất"
                };
            }

            // BƯỚC 3: Revoke tất cả tokens
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedDate = DateTime.UtcNow;
                token.LastModifiedDate = DateTime.UtcNow;
            }

            _unitOfWork.GetRepository<RefreshTokens>()
                .UpdateRange(activeTokens);

            await _unitOfWork.CommitAsync();

            _logger.Information(
                "All refresh tokens ({Count}) revoked for account {AccountId}",
                activeTokens.Count(),
                accountId);

            return new ApiResponse
            {
                Status = StatusCodes.Status200OK,
                Message = $"Đã đăng xuất khỏi {activeTokens.Count()} thiết bị thành công"
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Warning(ex, "Unauthorized logout all devices attempt");
            throw;
        }

        catch (Exception ex)
        {
            _logger.Error(ex, "Error during logout all devices");
            throw new Exception("Không thể đăng xuất tất cả thiết bị. Vui lòng thử lại.");
        }
    }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Hangfire;

namespace ECommerceManagementSystem.Coffee.Application.Jobs;

/// <summary>
/// Background job dọn dẹp RefreshTokens không còn dùng được.
///
/// Xóa token khi thỏa BẤT KỲ điều kiện nào sau:
///   1. IsRevoked = true  (đã bị thu hồi thủ công — logout, đổi mật khẩu, v.v.)
///   2. ExpiryDate &lt;= now (đã hết hạn tự nhiên)
///
/// Lý do xóa luôn token expired (không chỉ revoked):
///   Token hết hạn không bao giờ được dùng lại → giữ trong DB
///   chỉ tốn dung lượng và làm chậm query tìm token hợp lệ.
///
/// Tần suất khuyến nghị: 1 lần/ngày lúc 02:00 UTC (ít traffic nhất).
/// </summary>
public class RefreshTokenSyncJob
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;

    public RefreshTokenSyncJob(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 600 })]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;
        _logger.Information("RefreshTokenSyncJob started at {Now} UTC", now);

        try
        {
            // Load tất cả tokens cần xóa trong 1 query:
            // IsRevoked = true  → đã thu hồi
            // ExpiryDate <= now → đã hết hạn
            var tokensToDelete = await _unitOfWork
                .GetRepository<RefreshTokens>()
                .GetListAsync(predicate: x =>
                    x.IsRevoked || x.ExpiryDate <= now
                );

            if (!tokensToDelete.Any())
            {
                _logger.Information("RefreshTokenSyncJob: Không có token nào cần xóa.");
                return;
            }

            _unitOfWork.GetRepository<RefreshTokens>().DeleteRangeAsync(tokensToDelete);
            await _unitOfWork.CommitAsync();

            _logger.Information(
                "RefreshTokenSyncJob completed: đã xóa {Count} token(s). " +
                "Revoked: {Revoked}, Expired: {Expired}",
                tokensToDelete.Count,
                tokensToDelete.Count(t => t.IsRevoked),
                tokensToDelete.Count(t => t.ExpiryDate <= now)
            );
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "RefreshTokenSyncJob failed");
            throw; // Re-throw để Hangfire retry
        }
    }
}
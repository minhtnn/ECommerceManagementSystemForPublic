using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Hangfire;

namespace ECommerceManagementSystem.Coffee.Application.Jobs;

/// <summary>
/// Background job chạy định kỳ để đồng bộ trạng thái Promotion theo thời gian.
///
/// Logic:
///   Pending  → Active  : now >= StartDate  (promotion đến giờ bắt đầu)
///   Pending  → Expired : now >= EndDate    (job bị delay, bỏ qua Active)
///   Active   → Expired : now >= EndDate    (promotion hết hạn)
///
/// KHÔNG dùng .Date comparison vì mất thông tin giờ phút —
/// promotion set lúc 10:00 sẽ bị active từ 00:00 cùng ngày nếu dùng .Date.
/// </summary>
public class PromotionStatusSyncJob
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger _logger;

    public PromotionStatusSyncJob(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Entry point được Hangfire gọi.
    /// Đăng ký trong Program.cs:
    ///   RecurringJob.AddOrUpdate&lt;PromotionStatusSyncJob&gt;(
    ///       "promotion-status-sync",
    ///       job => job.ExecuteAsync(),
    ///       Cron.Minutely   // hoặc "*/5 * * * *" cho mỗi 5 phút
    ///   );
    /// </summary>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 30, 60, 120 })]
    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        var activatedCount = 0;
        var expiredCount = 0;

        try
        {
            // ─── 1. Pending → Active hoặc Expired ─────────────────────
            // Load tất cả Pending promotions mà StartDate đã đến
            var pendingToProcess = await _unitOfWork
                .GetRepository<Domain.Entities.PromotionRules>()
                .GetListAsync(predicate: x =>
                    x.Status == EPromotionStatus.Pending
                    && x.StartDate.HasValue
                    && x.StartDate.Value <= now
                );

            foreach (var promotion in pendingToProcess)
            {
                // Edge case: job bị delay → StartDate đã qua nhưng EndDate cũng đã qua
                // → chuyển thẳng Pending → Expired, bỏ qua Active
                if (promotion.EndDate.HasValue && promotion.EndDate.Value <= now)
                {
                    promotion.Status = EPromotionStatus.Expired;
                    promotion.LastModifiedDate = now;
                    _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
                        .UpdateAsync(promotion);

                    expiredCount++;
                }
                else
                {
                    promotion.Status = EPromotionStatus.Active;
                    promotion.LastModifiedDate = now;
                    _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
                        .UpdateAsync(promotion);

                    activatedCount++;
                }
            }

            // ─── 2. Active → Expired ───────────────────────────────────
            // Load tất cả Active promotions mà EndDate đã qua
            var activeToExpire = await _unitOfWork
                .GetRepository<Domain.Entities.PromotionRules>()
                .GetListAsync(predicate: x =>
                    x.Status == EPromotionStatus.Active
                    && x.EndDate.HasValue
                    && x.EndDate.Value <= now
                );

            foreach (var promotion in activeToExpire)
            {
                promotion.Status = EPromotionStatus.Expired;
                promotion.LastModifiedDate = now;
                _unitOfWork.GetRepository<Domain.Entities.PromotionRules>()
                    .UpdateAsync(promotion);

                expiredCount++;
            }

            // ─── 3. Lưu tất cả thay đổi 1 lần ────────────────────────
            if (activatedCount > 0 || expiredCount > 0)
            {
                await _unitOfWork.CommitAsync();
            }
            else
            {
            }
        }
        catch (Exception ex)
        {
            throw;
        }
    }
}
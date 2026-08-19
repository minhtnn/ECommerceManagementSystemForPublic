using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Jobs.DailySalesAggregateJobs;

/// <summary>
/// Chạy mỗi đêm, aggregate dữ liệu bán hàng của ngày hôm qua.
/// Chỉ tính đơn hàng có OrderStatus = Delivered.
///
/// Đăng ký trong Program.cs:
///   RecurringJob.AddOrUpdate<DailySalesAggregateJob>(
///       "daily-sales-aggregate",
///       job => job.ExecuteAsync(null),
///       "0 2 * * *"   // 2:00 AM UTC
///   );
///
/// Re-run thủ công cho ngày cụ thể:
///   BackgroundJob.Enqueue<DailySalesAggregateJob>(
///       job => job.ExecuteAsync(new DateOnly(2025, 6, 1))
///   );
/// </summary>
public class DailySalesAggregateJob
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger<DailySalesAggregateJob> _logger;

    public DailySalesAggregateJob(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger<DailySalesAggregateJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ExecuteAsync(DateOnly? targetDate = null)
    {
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _logger.LogInformation("[DailySalesAggregateJob] Start for date {Date}", date);

        try
        {
            // Lấy tất cả đơn Completed trong ngày, chưa aggregate
            // (nếu truyền targetDate thủ công thì bỏ điều kiện IsAggregated
            //  để cho phép re-run ghi đè)
            var isManualRun = targetDate.HasValue;

            var completedOrders = await _unitOfWork
                .GetRepository<Orders>()
                .GetListAsync(
                    predicate: o =>
                        o.OrderStatus == EOrderStatus.Delivered
                        && DateOnly.FromDateTime(o.CreatedDate) == date
                        && (isManualRun || !o.IsAggregated),
                    include: q => q
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product).ThenInclude(p => p.ProductImages)
                        .Include(o => o.AppliedOrderPromotions)
                        .ThenInclude(ap => ap.PromotionRule)
                );

            if (!completedOrders.Any())
            {
                _logger.LogInformation("[DailySalesAggregateJob] No completed orders for {Date}", date);
                return;
            }

            await AggregateProductSalesAsync(completedOrders, date);
            await AggregatePromotionStatsAsync(completedOrders, date);
            await MarkOrdersAsAggregatedAsync(completedOrders);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "[DailySalesAggregateJob] Done for {Date}. Orders processed: {Count}",
                date, completedOrders.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DailySalesAggregateJob] Failed for date {Date}", date);
            throw;
        }
    }

    // ─── Aggregate sản phẩm ───────────────────────────────────────────────────

    private async Task AggregateProductSalesAsync(ICollection<Orders> orders, DateOnly date)
    {
        var allDetails = orders.SelectMany(o => o.OrderDetails).ToList();

        // Group theo ProductId
        var grouped = allDetails.GroupBy(od => od.ProductId);

        foreach (var group in grouped)
        {
            var sample = group.First();
            var nonGiftItems = group.Where(od => !od.IsGiftItem).ToList();
            var giftItems = group.Where(od => od.IsGiftItem).ToList();

            var incoming = new DailyProductSales
            {
                ProductId = group.Key,
                ProductNameSnapshot = sample.ProductNameSnapshot
                                      ?? sample.Product?.Name
                                      ?? string.Empty,
                ProductImagePath = (sample.Product != null && sample.Product.ProductImages.Any())
                    ? sample.Product?.ProductImages.SingleOrDefault(x => x.IsMainImage).ImageUrl
                    : null,
                SaleDate = date,
                TotalQuantitySold = nonGiftItems.Sum(od => od.Quantity),
                TotalGiftQuantity = giftItems.Sum(od => od.Quantity),
                TotalRevenueGross = nonGiftItems.Sum(od => od.TotalPriceSnapshot),
                TotalOrderCount = group.Select(od => od.OrderId).Distinct().Count(),
            };

            await UpsertDailyProductSalesAsync(incoming);
        }
    }

    private async Task UpsertDailyProductSalesAsync(DailyProductSales incoming)
    {
        var existing = await _unitOfWork
            .GetRepository<DailyProductSales>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    x.ProductId == incoming.ProductId
                    && x.SaleDate == incoming.SaleDate);

        if (existing is null)
        {
            await _unitOfWork.GetRepository<DailyProductSales>().InsertAsync(incoming);
        }
        else
        {
            // Ghi đè toàn bộ — idempotent khi re-run
            existing.ProductNameSnapshot = incoming.ProductNameSnapshot;
            existing.TotalQuantitySold = incoming.TotalQuantitySold;
            existing.TotalGiftQuantity = incoming.TotalGiftQuantity;
            existing.TotalRevenueGross = incoming.TotalRevenueGross;
            existing.TotalOrderCount = incoming.TotalOrderCount;
            existing.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<DailyProductSales>().UpdateAsync(existing);
        }
    }

    // ─── Aggregate khuyến mãi ─────────────────────────────────────────────────

    private async Task AggregatePromotionStatsAsync(ICollection<Orders> orders, DateOnly date)
    {
        var allApplied = orders.SelectMany(o => o.AppliedOrderPromotions).ToList();

        if (!allApplied.Any()) return;

        // Group theo PromotionRuleId
        var grouped = allApplied.GroupBy(ap => ap.PromotionRuleId);

        foreach (var group in grouped)
        {
            var sample = group.First();

            // Lấy distinct orders để tính doanh thu — tránh double count
            var distinctOrders = group
                .Select(ap => ap.Order!)
                .DistinctBy(o => o.Id)
                .ToList();

            var incoming = new DailyPromotionStats
            {
                PromotionRuleId = group.Key,
                PromotionNameSnapshot = sample.PromotionRuleNameSnapshot
                                        ?? sample.PromotionRule?.Name
                                        ?? string.Empty,
                StatDate = date,
                TotalDiscountIssued = group.Sum(ap => ap.DiscountAmountApplied),
                TotalOrdersUsed = distinctOrders.Count,
                TotalRevenueWithPromo = distinctOrders.Sum(o => o.TotalAmount),
            };

            await UpsertDailyPromotionStatsAsync(incoming);
        }
    }

    private async Task UpsertDailyPromotionStatsAsync(DailyPromotionStats incoming)
    {
        var existing = await _unitOfWork
            .GetRepository<DailyPromotionStats>()
            .SingleOrDefaultAsync(
                predicate: x =>
                    x.PromotionRuleId == incoming.PromotionRuleId
                    && x.StatDate == incoming.StatDate);

        if (existing is null)
        {
            await _unitOfWork.GetRepository<DailyPromotionStats>().InsertAsync(incoming);
        }
        else
        {
            existing.PromotionNameSnapshot = incoming.PromotionNameSnapshot;
            existing.TotalDiscountIssued = incoming.TotalDiscountIssued;
            existing.TotalOrdersUsed = incoming.TotalOrdersUsed;
            existing.TotalRevenueWithPromo = incoming.TotalRevenueWithPromo;
            existing.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<DailyPromotionStats>().UpdateAsync(existing);
        }
    }

    // ─── Đánh dấu đơn đã xử lý ───────────────────────────────────────────────

    private Task MarkOrdersAsAggregatedAsync(ICollection<Orders> orders)
    {
        foreach (var order in orders.Where(o => !o.IsAggregated))
        {
            order.IsAggregated = true;
            order.AggregatedAt = DateTime.UtcNow;
            _unitOfWork.GetRepository<Orders>().UpdateAsync(order);
        }

        return Task.CompletedTask;
    }
}
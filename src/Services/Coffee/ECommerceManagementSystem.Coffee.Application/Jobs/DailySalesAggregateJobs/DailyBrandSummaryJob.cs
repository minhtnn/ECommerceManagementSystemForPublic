using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using ECommerceManagementSystem.Coffee.Infrastructure.Persistence;
using ECommerceManagementSystem.Coffee.Infrastructure.Repositories.Interface;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Application.Jobs.DailySalesAggregateJobs;

/// <summary>
/// Chạy mỗi đêm, aggregate doanh thu theo brand cho ngày hôm qua.
/// Đăng ký:
///   RecurringJob.AddOrUpdate<DailyBrandSummaryJob>(
///       "daily-brand-summary",
///       job => job.ExecuteAsync(null),
///       "0 2 * * *"
///   );
/// Re-run thủ công: job.ExecuteAsync(new DateOnly(2025, 6, 1))
/// </summary>
public class DailyBrandSummaryJob
{
    private readonly IUnitOfWork<ECommerceManagementSystemCoffeeContext> _unitOfWork;
    private readonly ILogger<DailyBrandSummaryJob> _logger;

    public DailyBrandSummaryJob(
        IUnitOfWork<ECommerceManagementSystemCoffeeContext> unitOfWork,
        ILogger<DailyBrandSummaryJob> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 120, 300 })]
    public async Task ExecuteAsync(DateOnly? targetDate = null)
    {
        var date = targetDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        _logger.LogInformation("[DailyBrandSummaryJob] Start for {Date}", date);

        try
        {
            // Lấy toàn bộ đơn trong ngày, đủ status
            // Join qua Customer để lấy BrandId
            var orders = await _unitOfWork.GetRepository<Orders>()
                .GetListAsync(
                    predicate: o =>
                        DateOnly.FromDateTime(o.CreatedDate) == date
                        && (o.OrderStatus == EOrderStatus.Delivered
                            || o.OrderStatus == EOrderStatus.Shipped
                            || o.OrderStatus == EOrderStatus.Processing
                            || o.OrderStatus == EOrderStatus.Pending),
                    include: q => q
                        .Include(o => o.Customer)
                        .Include(o => o.OrderDetails)
                );

            if (!orders.Any())
            {
                _logger.LogInformation("[DailyBrandSummaryJob] No orders for {Date}", date);
                return;
            }

            // Group theo BrandId
            var byBrand = orders.GroupBy(o => o.Customer!.BrandId);

            foreach (var brandGroup in byBrand)
            {
                var brandId = brandGroup.Key;
                var allOrders = brandGroup.ToList();

                // --- Tất cả đơn không Cancelled ---
                var completedOrders = allOrders; // đã filter status ở trên

                // --- Chỉ đơn Delivered ---
                var deliveredOrders = allOrders
                    .Where(o => o.OrderStatus == EOrderStatus.Delivered)
                    .ToList();

                var deliveredDetails = deliveredOrders
                    .SelectMany(o => o.OrderDetails)
                    .Where(od => !od.IsGiftItem)
                    .ToList();

                var incoming = new BrandDailySummary
                {
                    BrandId = brandId,
                    SummaryDate = date,

                    // Thông số 7, 8, 9
                    TotalRevenueGross = completedOrders.Sum(o => o.TotalAmountWithoutDiscount),
                    TotalDiscount = completedOrders.Sum(o => o.TotalOrderDiscount),
                    TotalRevenueNet = completedOrders.Sum(o => o.TotalAmount),
                    TotalOrderCount = completedOrders.Count,

                    // Thông số 10, 11
                    TotalRevenueGrossDelivered = deliveredOrders.Sum(o => o.TotalAmountWithoutDiscount),
                    TotalDiscountDelivered = deliveredOrders.Sum(o => o.TotalOrderDiscount),
                    TotalRevenueNetDelivered = deliveredOrders.Sum(o => o.TotalAmount),
                    TotalOrderCountDelivered = deliveredOrders.Count,
                    TotalQuantitySoldDelivered = deliveredDetails.Sum(od => od.Quantity),
                };

                await UpsertAsync(incoming);
            }

            await _unitOfWork.CommitAsync();
            _logger.LogInformation("[DailyBrandSummaryJob] Done for {Date}", date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DailyBrandSummaryJob] Failed for {Date}", date);
            throw;
        }
    }

    private async Task UpsertAsync(BrandDailySummary incoming)
    {
        var existing = await _unitOfWork.GetRepository<BrandDailySummary>()
            .SingleOrDefaultAsync(
                predicate: x => x.BrandId == incoming.BrandId
                                && x.SummaryDate == incoming.SummaryDate
            );

        if (existing is null)
        {
            await _unitOfWork.GetRepository<BrandDailySummary>().InsertAsync(incoming);
        }
        else
        {
            existing.TotalRevenueGross = incoming.TotalRevenueGross;
            existing.TotalDiscount = incoming.TotalDiscount;
            existing.TotalRevenueNet = incoming.TotalRevenueNet;
            existing.TotalOrderCount = incoming.TotalOrderCount;
            existing.TotalRevenueGrossDelivered = incoming.TotalRevenueGrossDelivered;
            existing.TotalDiscountDelivered = incoming.TotalDiscountDelivered;
            existing.TotalRevenueNetDelivered = incoming.TotalRevenueNetDelivered;
            existing.TotalOrderCountDelivered = incoming.TotalOrderCountDelivered;
            existing.TotalQuantitySoldDelivered = incoming.TotalQuantitySoldDelivered;
            existing.LastModifiedDate = DateTime.UtcNow;
            _unitOfWork.GetRepository<BrandDailySummary>().UpdateAsync(existing);
        }
    }
}
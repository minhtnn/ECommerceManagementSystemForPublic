using ECommerceManagementSystem.Coffee.Domain.Entities.Commons;

namespace ECommerceManagementSystem.Coffee.Domain.Entities;

public class DailyPromotionStats : EntityAuditBase<Guid>
{
    public required Guid PromotionRuleId { get; set; }
    public required string PromotionNameSnapshot { get; set; } // snapshot tại thời điểm aggregate
    public required DateOnly StatDate { get; set; }

    // Đo lường 1: Tổng tiền giảm đã phát ra
    public decimal TotalDiscountIssued { get; set; }

    // Đo lường 2: Số đơn hàng dùng KM
    public int TotalOrdersUsed { get; set; }

    // Đo lường 3: Hiệu quả từng chương trình (doanh thu thực tế đi kèm)
    public decimal TotalRevenueWithPromo { get; set; }  // SUM(TotalAmount) của đơn dùng KM

    // Đo lường 4: KM nào được dùng nhiều nhất → so sánh TotalOrdersUsed giữa các KM

    public virtual PromotionRules? PromotionRule { get; set; }
}
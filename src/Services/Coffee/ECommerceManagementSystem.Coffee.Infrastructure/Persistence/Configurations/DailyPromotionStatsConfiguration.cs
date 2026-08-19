using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class DailyPromotionStatsConfiguration : IEntityTypeConfiguration<DailyPromotionStats>
{
    public void Configure(EntityTypeBuilder<DailyPromotionStats> builder)
    {
        builder.ToTable("DailyPromotionStats");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PromotionRuleId).IsRequired();
        builder.Property(x => x.PromotionNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(x => x.StatDate).IsRequired();
        builder.Property(x => x.TotalDiscountIssued).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalOrdersUsed).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalRevenueWithPromo).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        // Unique key để upsert an toàn
        builder.HasIndex(x => new { x.PromotionRuleId, x.StatDate })
            .IsUnique()
            .HasDatabaseName("UIX_DailyPromotionStats_Promotion_Date");

        // Index cho query báo cáo theo khoảng ngày
        builder.HasIndex(x => x.StatDate)
            .HasDatabaseName("IX_DailyPromotionStats_StatDate");

        builder.HasOne(x => x.PromotionRule)
            .WithMany()
            .HasForeignKey(x => x.PromotionRuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
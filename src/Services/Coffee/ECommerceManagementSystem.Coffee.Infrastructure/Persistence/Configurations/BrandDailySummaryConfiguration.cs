using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class BrandDailySummaryConfiguration : IEntityTypeConfiguration<BrandDailySummary>
{
    public void Configure(EntityTypeBuilder<BrandDailySummary> builder)
    {
        builder.ToTable("BrandDailySummary");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.SummaryDate).IsRequired();

        foreach (var col in new[] {
                     nameof(BrandDailySummary.TotalRevenueGross),
                     nameof(BrandDailySummary.TotalDiscount),
                     nameof(BrandDailySummary.TotalRevenueNet),
                     nameof(BrandDailySummary.TotalRevenueGrossDelivered),
                     nameof(BrandDailySummary.TotalDiscountDelivered),
                     nameof(BrandDailySummary.TotalRevenueNetDelivered),
                 })
            builder.Property(col).HasColumnType("decimal(18,2)").IsRequired();

        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        // Upsert key
        builder.HasIndex(x => new { x.BrandId, x.SummaryDate })
            .IsUnique()
            .HasDatabaseName("UIX_BrandDailySummary_Brand_Date");

        builder.HasIndex(x => x.SummaryDate)
            .HasDatabaseName("IX_BrandDailySummary_SummaryDate");

        builder.HasOne(x => x.Brand).WithMany()
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
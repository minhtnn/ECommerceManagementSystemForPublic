using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class DailyProductSalesConfiguration : IEntityTypeConfiguration<DailyProductSales>
{
    public void Configure(EntityTypeBuilder<DailyProductSales> builder)
    {
        builder.ToTable("DailyProductSales");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ProductNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ProductImagePath);
        builder.Property(x => x.SaleDate).IsRequired();
        builder.Property(x => x.TotalQuantitySold).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalGiftQuantity).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.TotalRevenueGross).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalOrderCount).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        // Unique key để upsert an toàn
        builder.HasIndex(x => new { x.ProductId, x.SaleDate })
            .IsUnique()
            .HasDatabaseName("UIX_DailyProductSales_Product_Date");

        // Index cho query báo cáo theo khoảng ngày
        builder.HasIndex(x => x.SaleDate)
            .HasDatabaseName("IX_DailyProductSales_SaleDate");

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
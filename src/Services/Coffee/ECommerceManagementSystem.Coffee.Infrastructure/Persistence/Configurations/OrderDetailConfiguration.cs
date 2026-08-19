using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class OrderDetailConfiguration : IEntityTypeConfiguration<OrderDetails>
{
    public void Configure(EntityTypeBuilder<OrderDetails> builder)
    {
        builder.ToTable("OrderDetails");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.ProductNameSnapshot);
        builder.Property(x => x.IsGiftItem);
        builder.Property(x => x.GiftFromPromotionId);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPriceSnapshot).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalPriceSnapshot).HasColumnType("decimal(18,2)").IsRequired();
        builder.HasOne(x => x.Order).WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Product).WithMany(x => x.OrderDetails)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.OrderId, x.ProductId, x.IsGiftItem }).IsUnique();
    }
}
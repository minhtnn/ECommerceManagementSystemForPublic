using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Orders>
{
    public void Configure(EntityTypeBuilder<Orders> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_Orders_Code");
        builder.Property(x => x.OrderStatus).IsRequired().HasConversion(
            v => v.ToString(),
            v => (EOrderStatus)Enum.Parse(typeof(EOrderStatus), v)
        );
        builder.Property(x => x.PaymentStatus).IsRequired().HasConversion(
            v => v.ToString(),
            v => (EPaymentStatus)Enum.Parse(typeof(EPaymentStatus), v)
        );
        builder.Property(x => x.TotalAmountWithoutDiscount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalOrderDiscount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalOrderShippingFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ShippingAddress).HasMaxLength(int.MaxValue);
        builder.Property(x => x.ShippingContact).HasMaxLength(20);
        builder.Property(x => x.CustomerNote).HasMaxLength(500);
        builder.Property(x => x.PaymentUrl);
        builder.Property(x => x.QrCode);
        builder.Property(x => x.CancelledBy);
        builder.Property(x => x.CancelledByRole).HasConversion(
            v => v == null ? null : v.ToString(),
            v => string.IsNullOrEmpty(v) ? null : (ERole?)Enum.Parse(typeof(ERole), v)
        );
        builder.Property(x => x.CancelReason).HasMaxLength(500);
        builder.Property(x => x.CancelledAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new { x.CustomerId, x.Code }).IsUnique().HasDatabaseName("IX_Orders_CustomerOrderCode");
        builder.HasIndex(x => x.OrderStatus).HasDatabaseName("IX_Orders_OrderStatus");
        builder.HasIndex(x => x.PaymentStatus).HasDatabaseName("IX_Orders_PaymentStatus");
        builder.HasIndex(x => x.CancelledBy).HasDatabaseName("IX_Orders_CancelledBy");
        builder.Property(x => x.IsAggregated).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.AggregatedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new { x.OrderStatus, x.IsAggregated })
            .HasDatabaseName("IX_Orders_Status_IsAggregated");
        builder.HasOne(x => x.Customer).WithMany(x => x.Orders)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RefundRequest).WithOne(x => x.Order)
            .HasForeignKey<RefundRequests>(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.OrderHistoryStatuses).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.OrderDetails).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Payments).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.AppliedOrderPromotions).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.EmailNotifications).WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
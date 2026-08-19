using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payments>
{
    public void Configure(EntityTypeBuilder<Payments> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.PaymentMethodId).IsRequired();
        builder.Property(x => x.PaymentMethodCodeSnapshot).IsRequired();
        builder.Property(x => x.PaymentStatus).IsRequired().HasConversion(v => v.ToString(),
            v => (EPaymentStatus)Enum.Parse(typeof(EPaymentStatus), v));
        builder.Property(x => x.TransactionId);
        builder.Property(x => x.GateWayResponse).HasMaxLength(int.MaxValue);
        builder.Property(x => x.PaidAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_Payments_OrderId").IsUnique();
        builder.HasIndex(x => x.PaymentMethodId).HasDatabaseName("IX_Payments_PaymentMethodId");
        builder.HasIndex(x => x.PaymentStatus).HasDatabaseName("IX_Payments_PaymentStatus");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_Payments_CreatedDate");
        builder.HasIndex(x => x.TransactionId).HasDatabaseName("IX_Payments_TransactionId");

        builder.HasOne(x => x.PaymentMethod).WithMany(x => x.Payments)
            .HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
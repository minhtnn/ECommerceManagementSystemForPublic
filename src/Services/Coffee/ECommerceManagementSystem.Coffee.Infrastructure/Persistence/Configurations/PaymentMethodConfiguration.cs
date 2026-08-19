using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethods>
{
    public void Configure(EntityTypeBuilder<PaymentMethods> builder)
    {
        builder.ToTable("PaymentMethods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IDX_PaymentMethodCode");
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion(v => v.ToString(),
            v => (EPaymentMethodStatus)Enum.Parse(typeof(EPaymentMethodStatus), v));
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasMany(x => x.Payments).WithOne(x => x.PaymentMethod)
            .HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.BrandPaymentMethods).WithOne(x => x.PaymentMethods)
            .HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}
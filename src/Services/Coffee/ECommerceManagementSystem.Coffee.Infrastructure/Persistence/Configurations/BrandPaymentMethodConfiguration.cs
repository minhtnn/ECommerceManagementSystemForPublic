using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class BrandPaymentMethodConfiguration : IEntityTypeConfiguration<BrandPaymentMethods>
{
    public void Configure(EntityTypeBuilder<BrandPaymentMethods> builder)
    {
        builder.ToTable("BrandPaymentMethods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.PaymentMethodId).IsRequired();
        builder.Property(x => x.IsDefault);
        builder.Property(x => x.Configuration);
        builder.Property(x => x.IsActive).HasDefaultValue(false);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(1);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        
        builder.HasIndex(x => new {x.BrandId, x.PaymentMethodId}).HasDatabaseName("IX_BrandPaymentMethods_PaymentMethodId").IsUnique();
        builder.HasOne(x => x.Brand).WithMany(x => x.BrandPaymentMethods)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PaymentMethods).WithMany(x => x.BrandPaymentMethods)
            .HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}
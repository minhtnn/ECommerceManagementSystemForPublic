using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brands>
{
    public void Configure(EntityTypeBuilder<Brands> builder)
    {
        builder.ToTable("Brands");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IDX_Brands_Code");
        builder.Property(x => x.Name).IsRequired();
        builder.Property(x => x.Fullname).HasMaxLength(255);
        builder.Property(x => x.Slogan).HasMaxLength(500);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Address).IsRequired().HasMaxLength(255);
        builder.Property(x => x.PhoneNumber);
        builder.Property(x => x.LogoUrl);
        builder.Property(x => x.Configuration).HasMaxLength(int.MaxValue);
        builder.Property(x => x.Status).IsRequired().HasConversion(v => v.ToString(),
            v => (EBrandStatus)Enum.Parse(typeof(EBrandStatus), v));
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        
        builder.HasMany(x => x.ProductCategories).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Customers).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.PromotionRules).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Posts).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.BrandAccounts).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.BrandPaymentMethods).WithOne(x => x.Brand)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
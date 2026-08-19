using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategories>
{
    public void Configure(EntityTypeBuilder<ProductCategories> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.ParentProductCategoryId);
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IDX_ProductCategories_Code");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(255);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(1);
        builder.Property(x => x.Level).HasDefaultValue(1);
        builder.Property(x => x.IsLeafOnly).HasDefaultValue(true);
        builder.Property(x => x.IsDeletable).HasDefaultValue(true);
        builder.Property(x => x.ImageUrl);
        builder.Property(x => x.Status).IsRequired().HasConversion(
            v => v.ToString(), v => (ECategoryStatus)Enum.Parse(typeof(ECategoryStatus), v));
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new {x.BrandId, x.Code}).IsUnique().HasDatabaseName("IX_ProductCategories_BrandCode");
        builder.HasIndex(x => new {x.BrandId, x.ParentProductCategoryId}).HasDatabaseName("IX_ProductCategories_Hierarchy");
        
        builder.HasOne(x => x.Brand).WithMany(x => x.ProductCategories)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Parent).WithMany(x => x.Childrens)
            .HasForeignKey(x => x.ParentProductCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Childrens).WithOne(x => x.Parent)
            .HasForeignKey(x => x.ParentProductCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Products).WithOne(x => x.ProductCategory)
            .HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Products>
{
    public void Configure(EntityTypeBuilder<Products> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductCategoryId).IsRequired();
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_Products_Code");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FullName).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Price).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(x => x.ProductSellType).IsRequired().HasConversion(v => v.ToString(),
            v => (EProductSellType)Enum.Parse(typeof(EProductSellType), v)).HasDefaultValue(EProductSellType.ProductSell);
        builder.Property(x => x.Status).IsRequired().HasConversion(v => v.ToString(),
            v => (EProductStatus)Enum.Parse(typeof(EProductStatus), v));
        builder.Property(x => x.StockQuantity).HasDefaultValue(1);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(1);
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new {x.ProductCategoryId, x.Code}).IsUnique().HasDatabaseName("IX_ProductCategories_ProductCode");
        
        builder.HasOne(x => x.ProductCategory).WithMany(x => x.Products)
            .HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ProductImages).WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ProductSideAttributes).WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.OrderDetails).WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
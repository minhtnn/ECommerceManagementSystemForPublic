using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class ProductSideAttibuteConfiguration : IEntityTypeConfiguration<ProductSideAttributes>
{
    public void Configure(EntityTypeBuilder<ProductSideAttributes> builder)
    {
        builder.ToTable("ProductSideAttributes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductId).IsRequired();
        builder.Property(x => x.Key).HasMaxLength(200);
        builder.Property(x => x.Value).HasMaxLength(500);
        builder.HasOne(x => x.Product).WithMany(x => x.ProductSideAttributes)
            .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}
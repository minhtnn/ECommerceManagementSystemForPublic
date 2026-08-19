using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class BrandAccountConfiguration : IEntityTypeConfiguration<BrandAccounts>
{
    public void Configure(EntityTypeBuilder<BrandAccounts> builder)
    {
        builder.ToTable("BrandAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        
        builder.HasIndex(x => new {x.AccountId,  x.BrandId}).IsUnique().HasDatabaseName("IX_BrandAccounts_AccountId");
        builder.HasOne(x => x.Account).WithMany(x => x.BrandAccounts)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Brand).WithMany(x => x.BrandAccounts)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
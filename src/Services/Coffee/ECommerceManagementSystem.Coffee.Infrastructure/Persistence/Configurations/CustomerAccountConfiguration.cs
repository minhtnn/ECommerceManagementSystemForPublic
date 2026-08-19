using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class CustomerAccountConfiguration : IEntityTypeConfiguration<CustomerAccounts>
{
    public void Configure(EntityTypeBuilder<CustomerAccounts> builder)
    {
        builder.ToTable("CustomerAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasOne(x => x.Customer).WithMany(x => x.CustomerAccounts)
            .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Account).WithMany(x => x.CustomerAccounts)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshTokens>
{
    public void Configure(EntityTypeBuilder<RefreshTokens> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.Token).IsRequired();
        builder.Property(x => x.ExpiryDate).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.IsRevoked).HasDefaultValue(false);
        builder.Property(x => x.RevokedDate).HasColumnType("datetime2(3)");
        builder.Property(x => x.RevokedByIp);
        
        builder.HasOne(x => x.Account).WithMany(a => a.RefreshTokens)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
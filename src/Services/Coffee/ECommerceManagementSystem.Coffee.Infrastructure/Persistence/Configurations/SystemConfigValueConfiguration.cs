using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class SystemConfigValueConfiguration : IEntityTypeConfiguration<SystemConfigValues>
{
    public void Configure(EntityTypeBuilder<SystemConfigValues> builder)
    {
        builder.ToTable("SystemConfigValues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConfigKeyId).IsRequired();
        builder.Property(x => x.Value);
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        // Mỗi key chỉ có 1 value duy nhất ở system-level
        builder.HasIndex(x => x.ConfigKeyId)
            .IsUnique()
            .HasDatabaseName("IDX_SystemConfigValues_ConfigKeyId");

        builder.HasOne(x => x.ConfigKey)
            .WithMany(x => x.ConfigValues)
            .HasForeignKey(x => x.ConfigKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class SystemConfigKeyConfiguration : IEntityTypeConfiguration<SystemConfigKeys>
{
    public void Configure(EntityTypeBuilder<SystemConfigKeys> builder)
    {
        builder.ToTable("SystemConfigKeys");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Key).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique().HasDatabaseName("IDX_SystemConfigKeys_Key");
        builder.Property(x => x.Title).IsRequired().HasMaxLength(255);
        builder.Property(x => x.DataType).IsRequired().HasConversion(
            v => v.ToString(),
            v => (EConfigDataType)Enum.Parse(typeof(EConfigDataType), v));
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsRequired).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.IsSecure).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DefaultValue);
        builder.Property(x => x.DisplayOrder).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");

        builder.HasMany(x => x.ConfigValues)
            .WithOne(x => x.ConfigKey)
            .HasForeignKey(x => x.ConfigKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.TriggerDependencies)
            .WithOne(x => x.TriggerKey)
            .HasForeignKey(x => x.TriggerKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.DependentDependencies)
            .WithOne(x => x.DependentKey)
            .HasForeignKey(x => x.DependentKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
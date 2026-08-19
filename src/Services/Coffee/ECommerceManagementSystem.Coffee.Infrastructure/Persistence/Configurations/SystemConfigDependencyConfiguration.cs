using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class SystemConfigDependencyConfiguration : IEntityTypeConfiguration<SystemConfigDependencies>
{
    public void Configure(EntityTypeBuilder<SystemConfigDependencies> builder)
    {
        builder.ToTable("SystemConfigDependencies");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TriggerKeyId).IsRequired();
        builder.Property(x => x.TriggerValue).IsRequired();
        builder.Property(x => x.DependentKeyId).IsRequired();
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");

        builder.HasIndex(x => new { x.TriggerKeyId, x.TriggerValue, x.DependentKeyId })
            .IsUnique()
            .HasDatabaseName("IDX_SystemConfigDependencies_Unique");

        builder.HasOne(x => x.TriggerKey)
            .WithMany(x => x.TriggerDependencies)
            .HasForeignKey(x => x.TriggerKeyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DependentKey)
            .WithMany(x => x.DependentDependencies)
            .HasForeignKey(x => x.DependentKeyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
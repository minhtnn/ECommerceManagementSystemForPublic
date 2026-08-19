using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class RuleActionTargetConfiguration : IEntityTypeConfiguration<RuleActionTargets>
{
    public void Configure(EntityTypeBuilder<RuleActionTargets> builder)
    {
        builder.ToTable("RuleActionTargets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleActionId).IsRequired();
        builder.Property(x => x.TargetType).HasConversion(v => v.ToString(),
            v => (EActionTargetType)Enum.Parse(typeof(EActionTargetType), v));
        builder.Property(x => x.TargetId).IsRequired();
        builder.Property(x => x.Quantity).HasDefaultValue(1);
        builder.Property(x => x.Role).HasConversion(v => v.ToString(),
            v => (EActionTargetRole)Enum.Parse(typeof(EActionTargetRole), v));
        builder.HasOne(x => x.RuleAction).WithMany(x => x.RuleActionTargets)
            .HasForeignKey(x => x.RuleActionId).OnDelete(DeleteBehavior.Restrict);
    }
}
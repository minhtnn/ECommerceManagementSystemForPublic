using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class RuleConditionConfiguration : IEntityTypeConfiguration<RuleConditions>
{
    public void Configure(EntityTypeBuilder<RuleConditions> builder)
    {
        builder.ToTable("RuleConditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PromotionRuleId).IsRequired();
        builder.Property(x => x.ConditionType).IsRequired().HasConversion(
            v => v.ToString(), v => (ERuleConditionType)Enum.Parse(typeof(ERuleConditionType), v));
        builder.Property(x => x.Operator).IsRequired().HasConversion(
            v => v.ToString(), v => (ERuleConditionOperator)Enum.Parse(typeof(ERuleConditionOperator), v));
        builder.Property(x => x.Value).HasMaxLength(int.MaxValue);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.PromotionRuleId).HasDatabaseName("IX_PromotionRules_PromotionRuleId");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_RuleConditions_CreatedDate");

        builder.HasOne(x => x.PromotionRule).WithMany(x => x.RuleConditions)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}
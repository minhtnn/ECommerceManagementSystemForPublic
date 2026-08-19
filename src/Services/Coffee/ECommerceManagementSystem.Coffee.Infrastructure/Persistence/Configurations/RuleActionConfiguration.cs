using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class RuleActionConfiguration : IEntityTypeConfiguration<RuleActions>
{
    public void Configure(EntityTypeBuilder<RuleActions> builder)
    {
        builder.ToTable("RuleActions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PromotionRuleId).IsRequired();
        builder.Property(x => x.ActionType).IsRequired().HasConversion(
            v => v.ToString(), v => (ERuleActionType)Enum.Parse(typeof(ERuleActionType), v));
        builder.Property(x => x.Value).HasColumnType("varchar(MAX)");
        builder.Property(x => x.MaxDiscountAmountForPercentage).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.PromotionRuleId).HasDatabaseName("IX_RuleActions_PromotionRuleId");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_RuleActions_CreatedDate");

        builder.HasOne(x => x.PromotionRule).WithMany(x => x.RuleActions)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RuleActionTargets).WithOne(x => x.RuleAction)
            .HasForeignKey(x => x.RuleActionId).OnDelete(DeleteBehavior.Restrict);
    }
}
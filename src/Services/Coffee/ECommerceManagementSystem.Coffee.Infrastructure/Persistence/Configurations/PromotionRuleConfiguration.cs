using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class PromotionRuleConfiguration : IEntityTypeConfiguration<PromotionRules>
{
    public void Configure(EntityTypeBuilder<PromotionRules> builder)
    {
        builder.ToTable("PromotionRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_PromotionRules_Code");
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.ShortDescription).HasMaxLength(200);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.PromotionType).HasConversion(v => v.ToString(),
            v => (EPromotionType)Enum.Parse(typeof(EPromotionType), v));
        builder.Property(x => x.GlobalDiscountCap).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Priority).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.StartDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.EndDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.Status).HasConversion(v => v.ToString(),
            v => (EPromotionStatus)Enum.Parse(typeof(EPromotionStatus), v));
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new { x.BrandId, x.Code }).HasDatabaseName("IX_PromotionRules_BrandId_Code");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_PromotionRules_CreatedDate");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_PromotionRules_Status");

        builder.HasOne(x => x.Brand).WithMany(x => x.PromotionRules)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RuleConditions).WithOne(x => x.PromotionRule)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RuleActions).WithOne(x => x.PromotionRule)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.AppliedOrderPromotions).WithOne(x => x.PromotionRule)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
    }
}
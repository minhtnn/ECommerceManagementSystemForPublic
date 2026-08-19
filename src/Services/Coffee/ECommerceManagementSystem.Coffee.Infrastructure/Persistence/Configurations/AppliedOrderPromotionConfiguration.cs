using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class AppliedOrderPromotionConfiguration : IEntityTypeConfiguration<AppliedOrderPromotions>
{
    public void Configure(EntityTypeBuilder<AppliedOrderPromotions> builder)
    {
        builder.ToTable("AppliedOrderPromotions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PromotionRuleId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.PromotionRuleNameSnapshot);
        builder.Property(x => x.DiscountAmountApplied).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        
        builder.HasIndex(x => x.PromotionRuleId).HasDatabaseName("IX_PromotionRules_PromotionRuleId");
        builder.HasIndex(x => new {x.PromotionRuleId, x.OrderId}).IsUnique().HasDatabaseName("IX_PromotionRules_PromotionRuleId_Code");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_PromotionRules_CreatedDate");
        
        builder.HasOne(x => x.PromotionRule).WithMany(x => x.AppliedOrderPromotions)
            .HasForeignKey(x => x.PromotionRuleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Order).WithMany(x => x.AppliedOrderPromotions)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
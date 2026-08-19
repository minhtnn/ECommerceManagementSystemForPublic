using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class EmailNotificationConfiguration : IEntityTypeConfiguration<EmailNotifications>
{
    public void Configure(EntityTypeBuilder<EmailNotifications> builder)
    {
        builder.ToTable("EmailNotifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.EmailType).HasConversion(v => v.ToString(),
            v => (EEmailType)Enum.Parse(typeof(EEmailType), v)).IsRequired();
        builder.Property(x => x.RecipientEmail).IsRequired();
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.EmailBody).HasMaxLength(int.MaxValue);
        builder.Property(x => x.Status).IsRequired().HasConversion( v => v.ToString(),
            v => (EEmailStatus)Enum.Parse(typeof(EEmailStatus), v)).IsRequired();
        builder.Property(x => x.SentAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.FailedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.ErrorMessage).HasMaxLength(int.MaxValue);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        
        builder.HasIndex(x => x.OrderId).HasDatabaseName("IX_Posts_OrderId_Code");
        builder.HasIndex(x => x.EmailType).HasDatabaseName("IX_Posts_EmailType_Code");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_Posts_CreatedDate_Code");
        
        builder.HasOne(x => x.Order).WithMany(x => x.EmailNotifications)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
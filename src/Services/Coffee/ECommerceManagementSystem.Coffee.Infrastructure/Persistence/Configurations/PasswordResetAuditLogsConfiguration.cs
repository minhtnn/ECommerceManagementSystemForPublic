using ECommerceManagementSystem.Coffee.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class PasswordResetAuditLogsConfiguration : IEntityTypeConfiguration<PasswordResetAuditLogs>
{
    public void Configure(EntityTypeBuilder<PasswordResetAuditLogs> builder)
    {
        builder.ToTable("PasswordResetAuditLogs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AccountId).IsRequired();
        builder.Property(x => x.Action).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Action).HasDatabaseName("IX_PasswordResetAuditLogs_Action");
        builder.Property(x => x.PartialToken).HasMaxLength(16);
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => x.IpAddress).HasDatabaseName("IX_PasswordResetAuditLogs_IpAddress");
        builder.Property(x => x.UserAgent).HasMaxLength(500);
        builder.Property(x => x.Success).IsRequired();
        builder.HasIndex(x => x.Success).HasDatabaseName("IX_PasswordResetAuditLogs_Success");
        builder.Property(x => x.ErrorMessage).HasMaxLength(500);
        builder.Property(x => x.Metadata).HasColumnType("NVARCHAR(MAX)");
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)").HasDefaultValueSql("GETUTCDATE()");
        builder.HasIndex(x => new { x.AccountId, x.CreatedDate })
            .HasDatabaseName("IX_PasswordResetAuditLogs_AccountId_CreatedDate").IsDescending(false, true);
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_PasswordResetAuditLogs_CreatedDate");
        builder.HasOne(x => x.Account).WithMany(x => x.PasswordResetAuditLogs)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Accounts>
{
    public void Configure(EntityTypeBuilder<Accounts> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Role).IsRequired();
        builder.HasIndex(x => x.Role).HasDatabaseName("IX_Accounts_RoleId");

        builder.Property(x => x.Username).HasMaxLength(100);
        builder.HasIndex(x => x.Username).IsUnique().HasDatabaseName("IX_Accounts_Username");
        builder.Property(x => x.PasswordHash).HasMaxLength(int.MaxValue);
        builder.Property(x => x.PasswordSalt).HasMaxLength(int.MaxValue);
        builder.Property(x => x.Status).IsRequired().HasConversion(x => x.ToString(),
            v => (EAccountStatus)Enum.Parse(typeof(EAccountStatus), v));
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_Accounts_Status");
        builder.Property(x => x.PhoneNumber);
        builder.Property(x => x.IsPhoneVerified).HasDefaultValue(false);
        builder.Property(x => x.Email).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsEmailVerified).HasDefaultValue(false);
        builder.Property(x => x.EmailVerifiedDate).HasColumnType("datetime2(3)");
        builder.Property(x => x.EmailVerificationToken).HasMaxLength(100);
        builder.Property(x => x.EmailVerificationTokenExpiry).HasColumnType("datetime2(3)");
        builder.Property(x => x.LastOtpSentAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.GoogleId);
        builder.Property(x => x.AuthProvider)
            .HasConversion(
                v => v.HasValue ? v.Value.ToString() : null,
                v => string.IsNullOrEmpty(v)
                    ? (EAuthProvider?)null
                    : Enum.Parse<EAuthProvider>(v)
            );
        builder.Property(x => x.CreatedDate).IsRequired().HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.CreatedDate).HasDatabaseName("IX_Accounts_CreatedDate");
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.Property(x => x.PasswordResetToken).HasMaxLength(256);
        builder.HasIndex(x => x.PasswordResetToken).HasDatabaseName("IX_Accounts_PasswordResetToken")
            .HasFilter("[PasswordResetToken] IS NOT NULL");
        builder.Property(x => x.PasswordResetTokenExpiry).HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.PasswordResetTokenExpiry).HasDatabaseName("IX_Accounts_PasswordResetTokenExpiry")
            .HasFilter("[PasswordResetTokenExpiry] IS NOT NULL");
        builder.Property(x => x.PasswordResetTokenUsed).HasDefaultValue(false);
        builder.Property(x => x.PasswordResetTokenUsedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.PasswordResetFailedAttempts).HasDefaultValue(0);
        builder.Property(x => x.PasswordResetLockedUntil).HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.PasswordResetLockedUntil).HasDatabaseName("IX_Accounts_PasswordResetLockedUntil")
            .HasFilter("[PasswordResetLockedUntil] IS NOT NULL");
        builder.Property(x => x.PasswordLastChangedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.PasswordChangedCount).HasDefaultValue(0);

        builder.HasMany(x => x.BrandAccounts).WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.CustomerAccounts).WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.RefreshTokens).WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.PasswordResetAuditLogs).WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.PasswordResetAuditLogs).WithOne(x => x.Account)
            .HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
    }
}
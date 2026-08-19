using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequests>
{
    public void Configure(EntityTypeBuilder<RefundRequests> builder)
    {
        builder.ToTable("RefundRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.RefundAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Status).IsRequired().HasConversion(
            v => v.ToString(), v => (ERefundStatus)Enum.Parse(typeof(ERefundStatus), v));

        builder.Property(x => x.Method).IsRequired().HasConversion(
            v => v.ToString(), v => (ERefundMethod)Enum.Parse(typeof(ERefundMethod), v));
        builder.Property(x => x.Mode).IsRequired().HasConversion(
            v => v.ToString(), v => (ERefundMode)Enum.Parse(typeof(ERefundMode), v));
        builder.Property(x => x.BankAccountNumber).HasMaxLength(50);
        builder.Property(x => x.BankAccountName).HasMaxLength(200);
        builder.Property(x => x.BankName).HasMaxLength(200);
        builder.Property(x => x.TransferProofImagePath).HasMaxLength(500);
        builder.Property(x => x.TransferProofImageUrl).HasMaxLength(500);
        builder.Property(x => x.TransferReference).HasMaxLength(100);
        builder.Property(x => x.PaymentGatewayTransactionId).HasMaxLength(200);
        builder.Property(x => x.RefundTransactionId).HasMaxLength(200);
        builder.Property(x => x.GatewayResponse).HasMaxLength(int.MaxValue);
        builder.Property(x => x.GatewayRefundFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualRefundAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.RequestedBy).IsRequired();
        builder.Property(x => x.RequestedByRole).IsRequired().HasConversion(
            v => v.ToString(), v => (ERole)Enum.Parse(typeof(ERole), v));
        builder.Property(x => x.RequestedAt).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.DueDate).HasColumnType("datetime2(3)");
        builder.Property(x => x.ProcessedBy);
        builder.Property(x => x.ProcessedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.CompletedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.RemindersSent).HasDefaultValue(0);
        builder.Property(x => x.CustomerConfirmedReceived);
        builder.Property(x => x.CustomerConfirmedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.AdminNote).HasMaxLength(1000);
        builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.LastErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => x.OrderId).IsUnique().HasDatabaseName("IX_RefundRequests_OrderId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_RefundRequests_Status");
        builder.HasIndex(x => x.Mode).HasDatabaseName("IX_RefundRequests_Mode");
        builder.HasIndex(x => x.RequestedBy).HasDatabaseName("IX_RefundRequests_RequestedBy");
        builder.HasIndex(x => x.DueDate).HasDatabaseName("IX_RefundRequests_DueDate");
        builder.HasIndex(x => x.PaymentGatewayTransactionId)
            .HasDatabaseName("IX_RefundRequests_PaymentGatewayTransactionId");

        builder.HasOne(x => x.Order).WithOne(x => x.RefundRequest)
            .HasForeignKey<RefundRequests>(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
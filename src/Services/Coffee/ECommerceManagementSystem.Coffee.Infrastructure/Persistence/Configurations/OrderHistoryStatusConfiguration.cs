using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class OrderHistoryStatusConfiguration : IEntityTypeConfiguration<OrderHistoryStatus>
{
    public void Configure(EntityTypeBuilder<OrderHistoryStatus> builder)
    {
        builder.ToTable("OrderHistoryStatus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.FromStatus).HasConversion(v => v.ToString(),
            v => (EOrderStatus)Enum.Parse(typeof(EOrderStatus), v)).IsRequired();
        builder.Property(x => x.ToStatus).HasConversion(v => v.ToString(),
            v => (EOrderStatus)Enum.Parse(typeof(EOrderStatus), v)).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(int.MaxValue);
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.HasOne(x => x.Order).WithMany(x => x.OrderHistoryStatuses)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
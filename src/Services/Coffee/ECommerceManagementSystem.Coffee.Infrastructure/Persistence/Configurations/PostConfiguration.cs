using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Posts>
{
    public void Configure(EntityTypeBuilder<Posts> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(p => p.Id);
        builder.Property(x => x.BrandId).IsRequired();
        builder.Property(x => x.Code).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("IX_Posts_Code");
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Author).HasMaxLength(100);
        builder.Property(x => x.Slug).HasMaxLength(int.MaxValue);
        builder.Property(x => x.Content).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Excerpt).HasColumnType("nvarchar(max)");
        builder.Property(x => x.FeaturedImage).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(x => x.PublishedAt).HasColumnType("datetime2(3)");
        builder.Property(x => x.Status).IsRequired().HasConversion(v => v.ToString(),
            v => (EPostStatus)Enum.Parse(typeof(EPostStatus), v));
        builder.Property(x => x.CreatedDate).HasColumnType("datetime2(3)").IsRequired();
        builder.Property(x => x.LastModifiedDate).HasColumnType("datetime2(3)");
        builder.HasIndex(x => new {x.BrandId, x.Code}).IsUnique().HasDatabaseName("IX_Posts_BrandId_Code");
        builder.HasOne(x => x.Brand).WithMany(x => x.Posts)
            .HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Cascade);
    }
}
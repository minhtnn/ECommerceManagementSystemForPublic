using ECommerceManagementSystem.Coffee.Domain.Entities;
using ECommerceManagementSystem.Coffee.Domain.Entities.Commons.Interface;
using ECommerceManagementSystem.Coffee.Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Persistence;

public class ECommerceManagementSystemCoffeeContext : DbContext
{
    public ECommerceManagementSystemCoffeeContext()
    {
    }

    public ECommerceManagementSystemCoffeeContext(DbContextOptions<ECommerceManagementSystemCoffeeContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accounts> Accounts { get; set; }
    public virtual DbSet<AppliedOrderPromotions> AppliedOrderPromotions { get; set; }
    public virtual DbSet<BrandAccounts> BrandAccounts { get; set; }
    public virtual DbSet<BrandPaymentMethods> BrandPaymentMethods { get; set; }
    public virtual DbSet<Brands> Brands { get; set; }
    public virtual DbSet<CustomerAccounts> CustomerAccounts { get; set; }
    public virtual DbSet<CustomerAddresses> CustomerAddresses { get; set; }
    public virtual DbSet<Customers> Customers { get; set; }
    public virtual DbSet<EmailNotifications> EmailNotifications { get; set; }
    public virtual DbSet<OrderDetails> OrderDetails { get; set; }
    public virtual DbSet<OrderHistoryStatus> OrderHistoryStatuses { get; set; }
    public virtual DbSet<Orders> Orders { get; set; }
    public virtual DbSet<PasswordResetAuditLogs> PasswordResetAuditLogs { get; set; }
    public virtual DbSet<PaymentMethods> PaymentMethods { get; set; }
    public virtual DbSet<Payments> Payments { get; set; }
    public virtual DbSet<Posts> Posts { get; set; }
    public virtual DbSet<ProductCategories> ProductCategories { get; set; }
    public virtual DbSet<ProductImages> ProductImages { get; set; }
    public virtual DbSet<Products> Products { get; set; }
    public virtual DbSet<ProductSideAttributes> ProductSideAttributes { get; set; }
    public virtual DbSet<PromotionRules> PromotionRules { get; set; }
    public virtual DbSet<RefreshTokens> RefreshTokens { get; set; }
    public virtual DbSet<RefundRequests> RefundRequests { get; set; }
    public virtual DbSet<RuleActions> RuleActions { get; set; }
    public virtual DbSet<RuleActionTargets> RuleActionTargets { get; set; }
    public virtual DbSet<RuleConditions> RuleConditions { get; set; }
    public virtual DbSet<SystemConfigKeys> SystemConfigKeys { get; set; }
    public virtual DbSet<SystemConfigValues> SystemConfigValues { get; set; }
    public virtual DbSet<SystemConfigDependencies> SystemConfigDependencies { get; set; }
    public virtual DbSet<DailyProductSales> DailyProductSales { get; set; }
    public virtual DbSet<DailyPromotionStats> DailyPromotionStats { get; set; }
    public virtual DbSet<BrandDailySummary> BrandDailySummary { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ECommerceManagementSystemCoffeeContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
        var modified = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Modified ||
                        e.State == EntityState.Added ||
                        e.State == EntityState.Deleted);

        foreach (var item in modified)
            switch (item.State)
            {
                case EntityState.Added:
                    if (item.Entity is IDateTracking addedEntity)
                    {
                        addedEntity.CreatedDate = TimeUtil.GetCurrentSEATime();
                        item.State = EntityState.Added;
                    }

                    break;
                case EntityState.Modified:
                    if (item.Entity is IDateTracking modifiedEntity)
                    {
                        Entry(item.Entity).Property("Id").IsModified = false;
                        modifiedEntity.LastModifiedDate = TimeUtil.GetCurrentSEATime();
                        item.State = EntityState.Modified;
                    }

                    break;
            }

        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }
}
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;

namespace ECommerceManagementSystem.Coffee.Infrastructure.Configurations;

public static class CacheConfig
{
    public static string EntityCachePrefix (string entityName) => $"{entityName.ToLowerInvariant()}";
    public static string EntityListCachePrefix (string entityName) => $"{entityName.ToLowerInvariant()}:list";
    public static string EntityByIdCachePrefix (string entityName, string id) => $"{entityName.ToLowerInvariant()}:getgid:{id}";
    public static string EntityDetailCachePrefix (string entityName, string id) => $"{entityName.ToLowerInvariant()}:detail:{id}";
    public static string EntityInvalidationLock(string entityName) => $"{entityName.ToLowerInvariant()}:invalidation:lock";
    public static string EntityInvalidationCounter(string entityName) => $"{entityName.ToLowerInvariant()}:invalidation:counter";
    

    public static readonly TimeSpan BrandsCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ProductCategoriesCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PostsCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ProductsCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan CustomersCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan CustomerAddressesCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PaymentMethodsCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PublicPaymentMethodsCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan PromotionRulesCacheTTL = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan ProductsSaleStatisticCacheTTL = TimeSpan.FromHours(24);
    public static readonly TimeSpan PromotionRulesStatisticCacheTTL = TimeSpan.FromHours(24);
    
    
    
    public static readonly TimeSpan DebouncingWindow = TimeSpan.FromSeconds(5);
    public static readonly int DebouncingThreshold = 3;
    public static readonly TimeSpan DebouncingLockTTL = TimeSpan.FromSeconds(10);
    
    public static readonly EOperationBeforeCache[] CriticalOperations = new[] 
    { 
        EOperationBeforeCache.BulkCreate,
        EOperationBeforeCache.Delete,
        EOperationBeforeCache.StatusChange,
        EOperationBeforeCache.BulkUpdate
    };
}
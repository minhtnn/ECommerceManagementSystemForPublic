using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;

namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface ICacheInvalidationService
{
    /// <summary>
    /// Invalidate list cache với debouncing logic
    /// </summary>
    Task<CacheOperationResult> InvalidateEntityCacheAsync(
        string lockKey,
        EOperationBeforeCache operation,
        string counterKey,
        string entityCachePrefix);
    
    /// <summary>
    /// Force clear tất cả list cache của entity
    /// </summary>
    Task<CacheOperationResult> ForceClearEntityCacheAsync(string entityCachePrefix);
    
    /// <summary>
    /// Get entity detail từ cache
    /// </summary>
    Task<T?> GetDetailFromCacheAsync<T>(string entityDetailCachePrefix) where T : class;
    
    /// <summary>
    /// Set entity detail vào cache
    /// </summary>
    Task<bool> SetDetailToCacheAsync<T>(
        string entityDetailCachePrefix,
        T entity,
        TimeSpan ttl) where T : class;
}
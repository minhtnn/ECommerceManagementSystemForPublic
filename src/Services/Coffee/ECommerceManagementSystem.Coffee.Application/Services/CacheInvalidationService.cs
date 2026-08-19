using System.Text.Json;
using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;
using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;
using ECommerceManagementSystem.Coffee.Infrastructure.Configurations;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class CacheInvalidationService : ICacheInvalidationService
{
    private readonly IRedisService _redisService;
    private readonly ILogger<CacheInvalidationService> _logger;

    public CacheInvalidationService(
        IRedisService redisService,
        ILogger<CacheInvalidationService> logger)
    {
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Invalidate cache với debouncing logic
    /// </summary>
    public async Task<CacheOperationResult> InvalidateEntityCacheAsync(string lockKey, EOperationBeforeCache operation,
        string counterKey, string entityCachePrefix)
    {
        try
        {
            var isCritical = CacheConfig.CriticalOperations.Contains(operation);

            if (isCritical)
            {
                _logger.LogInformation($"Critical operation {operation} - Invalidating cache immediately");
                return await ForceClearEntityCacheAsync(entityCachePrefix.ToLowerInvariant());
            }

            return await HandleDebouncedInvalidation(counterKey, lockKey, entityCachePrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating entity cache");
            return CacheOperationResult.Failure(
                "Failed to invalidate cache",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Xử lý debouncing - chỉ invalidate khi có đủ nhiều thay đổi
    /// </summary>
    private async Task<CacheOperationResult> HandleDebouncedInvalidation(string counterKey, string lockKey,
        string entityCachePrefix)
    {
        try
        {
            // Tăng counter (đếm số lần thay đổi)
            var count = await _redisService.IncrementAsync(counterKey);

            // Set TTL cho counter nếu là lần đầu tiên
            if (count == 1)
            {
                await _redisService.SetExpireAsync(counterKey, CacheConfig.DebouncingWindow);
                _logger.LogDebug($"Started debouncing window for entity cache invalidation");
            }

            _logger.LogDebug($"Entity cache invalidation counter: {count}/{CacheConfig.DebouncingThreshold}");

            // Kiểm tra xem đã đạt threshold chưa
            if (count >= CacheConfig.DebouncingThreshold)
            {
                // Thử acquire lock để tránh nhiều thread invalidate cùng lúc
                var lockAcquired = await _redisService.SetIfNotExistsAsync(
                    lockKey,
                    "locked",
                    CacheConfig.DebouncingLockTTL
                );

                if (lockAcquired)
                {
                    _logger.LogInformation($"Threshold reached ({count} changes) - Invalidating entity cache");

                    // Xóa cache
                    var clearResult = await ForceClearEntityCacheAsync(entityCachePrefix);

                    // Reset counter
                    await _redisService.DeleteKeyAsync(counterKey);
                    return CacheOperationResult.SuccessThresholdReached(
                        clearResult.DeletedKeysCount ?? 0
                    );
                }
                else
                {
                    _logger.LogDebug("Another process is already invalidating cache");
                    return CacheOperationResult.SuccessLockSkipped();
                }
            }
            else
            {
                _logger.LogDebug($"Debouncing: {count}/{CacheConfig.DebouncingThreshold} changes, waiting...");
                return CacheOperationResult.SuccessDebounced(
                    (int)count,
                    CacheConfig.DebouncingThreshold
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in debounced invalidation");
            return CacheOperationResult.Failure(
                "Failed to handle debounced invalidation",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Force clear tất cả entity cache (dùng DeleteKeysByPattern)
    /// </summary>
    public async Task<CacheOperationResult> ForceClearEntityCacheAsync(string entityCachePrefix)
    {
        try
        {
            var pattern = $"{entityCachePrefix}:*";
            _logger.LogDebug($"Attempting to clear cache with pattern: {pattern}");
            var deletedCount = await _redisService.DeleteKeysByPatternAsync(pattern);

            if (deletedCount > 0)
            {
                _logger.LogInformation($"Cleared {deletedCount} entity cache entries");
                return CacheOperationResult.SuccessImmediate((int)deletedCount);
            }
            else
            {
                _logger.LogDebug("No entity cache entries to clear");
                return CacheOperationResult.SuccessNoKeysFound();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error force clearing entity cache");
            return CacheOperationResult.Failure(
                "Failed to force clear cache",
                ex.Message
            );
        }
    }

    /// <summary>
    /// Get entity detail từ cache
    /// </summary>
    public async Task<T?> GetDetailFromCacheAsync<T>(string entityDetailCachePrefix) where T : class
    {
        try
        {
            var cachedData = await _redisService.GetStringAsync(entityDetailCachePrefix);

            if (string.IsNullOrEmpty(cachedData))
            {
                _logger.LogDebug($"Detail cache MISS: {entityDetailCachePrefix}");
                return null;
            }

            _logger.LogDebug($"Detail cache HIT: {entityDetailCachePrefix}");
            return JsonSerializer.Deserialize<T>(cachedData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting detail cache.");
            return null;
        }
    }

    /// <summary>
    /// Set entity detail vào cache
    /// </summary>
    public async Task<bool> SetDetailToCacheAsync<T>(
        string entityDetailCachePrefix,
        T entity,
        TimeSpan ttl) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(entity);

            // Dùng TTL mặc định nếu không truyền vào
            var cacheTTL = ttl;

            var success = await _redisService.SetStringAsync(entityDetailCachePrefix.ToLowerInvariant(), serialized, cacheTTL);

            if (success)
            {
                _logger.LogDebug($"Cached detail: {entityDetailCachePrefix} (TTL: {cacheTTL.TotalMinutes} min)");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error setting detail cache");
            return false;
        }
    }
}
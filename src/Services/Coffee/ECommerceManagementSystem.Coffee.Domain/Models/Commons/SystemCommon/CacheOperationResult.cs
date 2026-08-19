using ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemEnum;

namespace ECommerceManagementSystem.Coffee.Domain.Models.Commons.SystemCommon;

public class CacheOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public ECacheInvalidationType InvalidationType { get; set; }
    public int? DeletedKeysCount { get; set; }
    public string? ErrorDetails { get; set; }
    
    public static CacheOperationResult SuccessImmediate(int deletedCount)
    {
        return new CacheOperationResult
        {
            Success = true,
            Message = $"Cache invalidated immediately. Deleted {deletedCount} keys.",
            InvalidationType = ECacheInvalidationType.Immediate,
            DeletedKeysCount = deletedCount
        };
    }

    public static CacheOperationResult SuccessDebounced(int currentCount, int threshold)
    {
        return new CacheOperationResult
        {
            Success = true,
            Message = $"Change tracked. Count: {currentCount}/{threshold}. Waiting for threshold.",
            InvalidationType = ECacheInvalidationType.Debounced,
            DeletedKeysCount = null
        };
    }

    public static CacheOperationResult SuccessThresholdReached(int deletedCount)
    {
        return new CacheOperationResult
        {
            Success = true,
            Message = $"Threshold reached. Cache invalidated. Deleted {deletedCount} keys.",
            InvalidationType = ECacheInvalidationType.ThresholdReached,
            DeletedKeysCount = deletedCount
        };
    }

    public static CacheOperationResult SuccessNoKeysFound()
    {
        return new CacheOperationResult
        {
            Success = true,
            Message = "No cache keys found to delete.",
            InvalidationType = ECacheInvalidationType.NoKeys,
            DeletedKeysCount = 0
        };
    }

    public static CacheOperationResult SuccessLockSkipped()
    {
        return new CacheOperationResult
        {
            Success = true,
            Message = "Another process is already invalidating cache. Skipped.",
            InvalidationType = ECacheInvalidationType.LockSkipped,
            DeletedKeysCount = null
        };
    }

    public static CacheOperationResult Failure(string errorMessage, string? errorDetails = null)
    {
        return new CacheOperationResult
        {
            Success = false,
            Message = errorMessage,
            InvalidationType = ECacheInvalidationType.Failed,
            ErrorDetails = errorDetails
        };
    }
}
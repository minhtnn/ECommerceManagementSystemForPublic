namespace ECommerceManagementSystem.Coffee.Application.Services.Interface;

public interface IRedisService
{
    // List operations
    Task PushToListAsync(string key, string value);
    Task RemoveFromListAsync(string key, string value);
    Task<List<string>> GetListAsync(string key);
    
    // Hash operations
    Task SetHashAsync(string key, string field, string value);
    Task<string?> GetHashAsync(string key, string field);
    Task RemoveHashAsync(string key, string member);
    
    // Sorted Set operations
    Task<List<string>> GetSortedSetAsync(string key);
    Task SetSortedSetAsync(string key, string member, double score);
    Task RemoveSortedSetAsync(string key, string member);
    
    // String operations
    Task<string?> GetStringAsync(string key);
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    
    // Key operations
    Task<bool> KeyExistsAsync(string key);
    Task<bool> SetExpireAsync(string key, TimeSpan expiry);
    Task<bool> DeleteKeyAsync(string key);
    Task<long> DeleteKeysByPatternAsync(string pattern);
    
    // Atomic operations (cho debouncing)
    Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry);
    Task<long> IncrementAsync(string key);
    Task<long> DecrementAsync(string key);
}
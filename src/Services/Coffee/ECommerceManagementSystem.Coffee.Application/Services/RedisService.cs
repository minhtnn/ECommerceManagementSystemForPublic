using ECommerceManagementSystem.Coffee.Application.Services.Interface;
using StackExchange.Redis;

namespace ECommerceManagementSystem.Coffee.Application.Services;

public class RedisService : IRedisService
{
    private readonly IDatabase _db;
    private readonly IConnectionMultiplexer _redisConnection;

    public RedisService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
        _redisConnection = redis;
    }

    #region List operations

    public async Task PushToListAsync(string key, string value)
    {
        await _db.ListRightPushAsync(key, value);
    }

    public async Task RemoveFromListAsync(string key, string value)
    {
        await _db.ListRemoveAsync(key, value, 1);
    }

    public Task<List<string>> GetListAsync(string key)
    {
        return _db.ListRangeAsync(key)
            .ContinueWith(t => t.Result.Select(x => x.ToString()).ToList());
    }

    #endregion

    #region Hash operations

    public async Task SetHashAsync(string key, string field, string value)
    {
        await _db.HashSetAsync(key, field, value);
    }

    public async Task<string?> GetHashAsync(string key, string field)
    {
        var value = await _db.HashGetAsync(key, field);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task RemoveHashAsync(string key, string member)
    {
        await _db.HashDeleteAsync(key, member);
    }

    #endregion

    #region Sorted set operations

    public Task<List<string>> GetSortedSetAsync(string key)
    {
        return _db.SortedSetRangeByRankAsync(key, order: Order.Ascending)
            .ContinueWith(t => t.Result.Select(x => x.ToString()).ToList());
    }

    public async Task SetSortedSetAsync(string key, string member, double score)
    {
        await _db.SortedSetAddAsync(key, member, score);
    }

    public async Task RemoveSortedSetAsync(string key, string member)
    {
        await _db.SortedSetRemoveAsync(key, member);
    }

    #endregion

    #region String operations

    public async Task<string?> GetStringAsync(string key)
    {
        var value = await _db.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        return await _db.StringSetAsync(key, value, expiry.HasValue ? expiry.Value : TimeSpan.MaxValue);
    }

    #endregion

    #region Key operations

    public async Task<bool> KeyExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task<bool> SetExpireAsync(string key, TimeSpan expiry)
    {
        return await _db.KeyExpireAsync(key, expiry);
    }

    public async Task<bool> DeleteKeyAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    /// <summary>
    /// Xóa tất cả keys khớp với pattern (wildcard)
    /// VD: DeleteKeysByPatternAsync("brands:*") sẽ xóa tất cả keys bắt đầu bằng "brands:"
    /// </summary>
    public async Task<long> DeleteKeysByPatternAsync(string pattern)
    {
        try
        {
            var endpoints = _redisConnection.GetEndPoints();
            var server = _redisConnection.GetServer(endpoints.First());

            // Lấy tất cả keys khớp với pattern
            var keys = server.Keys(pattern: pattern).ToArray();

            if (!keys.Any())
                return 0;

            // Batch delete để tối ưu performance
            var tasks = new List<Task<bool>>();
            foreach (var key in keys)
            {
                tasks.Add(_db.KeyDeleteAsync(key));
            }

            await Task.WhenAll(tasks);

            return keys.Length;
        }
        catch (Exception)
        {
            throw;
        }
    }

    #endregion

    #region Automic operations

    /// <summary>
    /// Set giá trị chỉ khi key chưa tồn tại (NX - Not eXists)
    /// Dùng cho distributed locking trong debouncing
    /// </summary>
    public async Task<bool> SetIfNotExistsAsync(string key, string value, TimeSpan expiry)
    {
        return await _db.StringSetAsync(key, value, expiry, when: When.NotExists);
    }

    /// <summary>
    /// Tăng giá trị counter lên 1 (atomic operation)
    /// Trả về giá trị sau khi tăng
    /// </summary>
    public async Task<long> IncrementAsync(string key)
    {
        return await _db.StringIncrementAsync(key);
    }

    /// <summary>
    /// Giảm giá trị counter xuống 1 (atomic operation)
    /// Trả về giá trị sau khi giảm
    /// </summary>
    public async Task<long> DecrementAsync(string key)
    {
        return await _db.StringDecrementAsync(key);
    }

    #endregion
}
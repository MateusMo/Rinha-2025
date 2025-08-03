using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Infraestrutura.CacheRepositorio;

public class Cache : ICache
{
    private readonly IDistributedCache _distributedCache;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public Cache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedValue))
                return default(T);

            return JsonSerializer.Deserialize<T>(cachedValue, JsonOptions);
        }
        catch
        {
            return default(T);
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, int expiresInMinutes = 60)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiresInMinutes)
            };

            var serializedValue = JsonSerializer.Serialize(value, JsonOptions);
            await _distributedCache.SetStringAsync(key, serializedValue, options);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cachedValue = await _distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(cachedValue);
        }
        catch
        {
            return false;
        }
    }
}
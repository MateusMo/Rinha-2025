using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Infraestrutura.CacheRepositorio;

public class Cache : ICache
{
    private readonly IDistributedCache _distributedCache;

    public Cache(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T> GetAsync<T>(string key)
    {
        var cachedValue = await _distributedCache.GetStringAsync(key);
        if (cachedValue == null)
            return default(T);

        return JsonSerializer.Deserialize<T>(cachedValue);
    }

    public async Task<bool> SetAsync<T>(string key, T value, int expiresInMinutes = 60)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiresInMinutes)
            };

            var serializedValue = JsonSerializer.Serialize(value);
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
        var cachedValue = await _distributedCache.GetStringAsync(key);
        return cachedValue != null;
    }
}
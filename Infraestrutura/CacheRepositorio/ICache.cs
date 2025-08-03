namespace Infraestrutura.CacheRepositorio;

public interface ICache
{
    Task<T> GetAsync<T>(string key);
    Task<bool> SetAsync<T>(string key, T value, int expiresInMinutes = 60);
    Task<bool> RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
}
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace MyApp.Domain.Service.Service;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SaveAsync<T>(string key, T data, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public CacheService(IDistributedCache distributedCache)
    {
        _cache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        if (await _cache.GetAsync(key, ct) is not { } cacheValue) return default;

        using var ms = new MemoryStream(cacheValue);
        return await JsonSerializer.DeserializeAsync<T>(ms, options: JsonSerializerOptions.Default, ct);
    }

    public Task SaveAsync<T>(string key, T data, CancellationToken ct = default)
    {
        return _cache.SetAsync(
            key,
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                data,
                JsonSerializerOptions.Default)),
            ct);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        return _cache.RemoveAsync(key, ct);
    }
}
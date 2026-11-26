using ZiggyCreatures.Caching.Fusion;

namespace MyApp.Services.Caching;

/// <summary>
///     <see cref="ICacheService" /> on FusionCache, configured by <c>AddMyAppCache</c>: memory L1, Redis L2 and Redis
///     backplane. Stateless, registered as a singleton.
/// </summary>
internal sealed class FusionCacheService : ICacheService
{
    private readonly IFusionCache _cache;

    public FusionCacheService(IFusionCache fusionCache)
    {
        _cache = fusionCache;
    }

    /// <inheritdoc />
    public async ValueTask<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _cache.TryGetAsync<T>(key, token: ct);

        return value.HasValue
            ? new CacheResult<T>(true, value.Value)
            : CacheResult<T>.Miss;
    }

    /// <inheritdoc />
    public ValueTask<T> GetOrSetAsync<T>(string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default)
        => _cache.GetOrSetAsync<T>(key, async (_, token) => await factory(token), tags: tags, token: ct);

    /// <inheritdoc />
    public ValueTask SetAsync<T>(string key,
        T value,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default)
        => _cache.SetAsync(key, value, tags: tags, token: ct);

    /// <inheritdoc />
    public ValueTask RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, token: ct);

    /// <inheritdoc />
    public ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default)
        => _cache.RemoveByTagAsync(tag, token: ct);
}

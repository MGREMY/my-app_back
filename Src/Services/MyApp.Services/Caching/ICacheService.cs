namespace MyApp.Services.Caching;

/// <summary>
///     Application cache: in-memory (L1) on each instance, shared distributed (L2) cache, and removals propagated to
///     the in-memory cache of every instance. Services depend on this contract only, never on the caching library.
/// </summary>
/// <remarks>
///     Keys and tags are defined in <c>ServiceConstant.Cache</c>. A given key always stores the same type, and never an
///     EF Core entity: cache a dedicated model built with a projection.
/// </remarks>
public interface ICacheService
{
    /// <summary>
    ///     Reads <paramref name="key"/> without creating it; <see cref="CacheResult{T}.Miss" /> when absent.
    /// </summary>
    ValueTask<CacheResult<T>> TryGetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    ///     Reads <paramref name="key"/>, or runs <paramref name="factory"/> and stores its result when absent.
    ///     Concurrent calls for the same key on one instance run the factory once.
    /// </summary>
    /// <remarks>
    ///     A <c>null</c> result is cached too: the service that creates the data must overwrite the key with
    ///     <see cref="SetAsync{T}" />.
    /// </remarks>
    ValueTask<T> GetOrSetAsync<T>(string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Stores <paramref name="value"/> under <paramref name="key"/>, replacing the previous entry on every instance.
    /// </summary>
    ValueTask SetAsync<T>(string key, T value,
        IReadOnlyCollection<string>? tags = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Removes <paramref name="key"/> on every instance; does nothing when absent.
    /// </summary>
    ValueTask RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     Removes every entry tagged <paramref name="tag"/>; <c>"*"</c> removes every entry.
    /// </summary>
    ValueTask RemoveByTagAsync(string tag, CancellationToken ct = default);
}
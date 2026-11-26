namespace MyApp.Services.Caching;

/// <summary>Result of <see cref="ICacheService.TryGetAsync{T}" />: a cached value, or a miss.</summary>
/// <param name="HasValue">Whether the key was found. A cached <c>null</c> is a hit whose <paramref name="Value"/> is <c>null</c>.</param>
/// <param name="Value">The cached value; <c>default</c> on a miss, so read it only when <paramref name="HasValue"/> is <c>true</c>.</param>
public readonly record struct CacheResult<T>(bool HasValue, T Value)
{
    /// <summary>The key was not found.</summary>
    public static CacheResult<T> Miss => default;
}

# Cache

> **Read this when** a service reads from or writes to the cache, or you change how entries are invalidated.

The cache is [FusionCache](https://github.com/ZiggyCreatures/FusionCache), in three parts:

- an in-memory cache (**L1**) on each API instance;
- Redis as shared cache (**L2**);
- a Redis **backplane** that propagates every write and removal to the L1 of the other instances.

`AddMyAppCache()` configures it (keys prefixed with `myApp:`, entries kept 5 minutes), and `AddMyAppServices()`
registers `ICacheService` on top of it.

## Using it

- Inject `ICacheService` (`MyApp.Services.Caching`), never `IFusionCache` nor `HybridCache`: services do not depend
  on the caching library, only `FusionCacheService` does.
- Keys and tags are defined in `ServiceConstant.Cache` (`UserKey(id)`, `UserTag`).
- Read with `GetOrSetAsync`, which runs the factory once per instance for concurrent calls, or with `TryGetAsync` to
  read without creating (`CacheResult<T>`: `HasValue`, `Value`).
- Write with `SetAsync`, invalidate with `RemoveAsync` or `RemoveByTagAsync`. `Clear` removes every entry.

## Rules

- **Never cache EF entities.** Cache a dedicated model from `Src/Services/MyApp.Services/CacheModels/`, built with a
  projection (`UserProjection.ToCacheUser`).
- **A given key always stores the same type**, whichever service writes it.
- **`GetOrSetAsync` also caches a `null` result.** Make sure the service that creates the data overwrites the key.
- **Fail-safe** (serving an expired entry when the factory fails) is disabled. It can be enabled for business data,
  never for security data such as the BFF sessions.

## Accepted limits

- A change made outside of the services (directly in the database) stays invisible until the entry expires.
- The caching of `null` is still to be revisited.

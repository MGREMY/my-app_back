# Host composition and dependency injection

> **Read this when** you touch `Program.cs`, add a host concern (a middleware, a framework feature) or wonder why a
> new service is registered without any `AddScoped`.

`MyApp.Host.Api` is the composition root: it is the only place that decides which infrastructure is used. It stays
readable because `Program.cs` is a single fluent chain of `AddAppXxx()` / `UseAppXxx()` calls, each concern living in
its own file.

## Host configuration extensions

Each concern lives in `Src/Host/MyApp.Host.Api/Extension/Configuration/<Concern>Configuration.cs` and exposes C# 14+
`extension` blocks:

- `AddAppXxx()` on `TBuilder where TBuilder : IHostApplicationBuilder`, returning `TBuilder`;
- `UseAppXxx()` on `WebApplication`, returning `WebApplication`.

Both return the type they extend, so the chain stays typed. A framework method that returns `IApplicationBuilder`
(for example `UseStatusCodePages`) must be wrapped in a `UseAppXxx()` extension for the same reason.

**Settings.** An extension that needs settings reads them itself with `builder.GetAppConfig()` inside `AddAppXxx()`
(see [Configuration](configuration.md)). Do not create per-extension option classes, and do not pass settings from
`Program.cs` through an `Action<T>`: every setting belongs to `ApiOption`. When a `UseAppXxx()` needs a setting,
resolve it during `AddAppXxx()` instead. For example, CORS registers a default policy with the configured origins,
and `UseAppCors()` only calls `UseCors()`.

**Middleware order** in `Program.cs` matters:

1. CORS
2. exception handler
3. problem details
4. authentication
5. authorization
6. OpenAPI
7. endpoints

Problem details run before authentication so that the `401` / `403` it produces get a body (see
[Cross-cutting concerns](cross-cutting.md#errors)).

## Layer registration

Each layer exposes its own registration extension, and the host composes them:

```csharp
builder.Services
    .AddMyAppPersistence(config.Database.ConnectionString)   // Domain.Models: DbContext pool, Npgsql
    .AddMyAppCache(config.Cache.ConnectionString)            // Services: FusionCache + Redis L2 + Redis backplane
    .AddMyAppServices(options => { ... });                   // Services: use cases, ICacheService, file storage, options
```

A layer never registers infrastructure that belongs to another layer.

## Automatic service registration

You never register a use case by hand. `AddMyAppServices()` scans the assemblies:

- every non-generic interface of `MyApp.Services.Abstractions` is registered as **scoped**;
- it must have **exactly one** implementing class in `MyApp.Services`, otherwise startup fails with an
  `InvalidOperationException` (none found, or several found);
- open generic interfaces (`IService<>`, `IServiceAsync<,>`...) are excluded.

The consequence: only put an interface in `Services.Abstractions` if it is a service implemented in `Services`.

Infrastructure used by the services is not a use case. Its interface lives in `MyApp.Services`, next to its
implementation and out of the scan, and `AddMyAppServices()` registers it explicitly with the lifetime it needs:
`Caching/ICacheService` as a singleton, `IO/IFileStorageService` as scoped.

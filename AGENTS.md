# AGENTS.md

Reference guide for anyone working on this repository, human or AI agent. It describes the architecture, the
conventions in place and step-by-step recipes for the most common tasks. When in doubt, follow the existing code:
consistency with what is already there wins over personal preference.

## 1. Overview

Backend API template built with:

| Concern           | Technology                                                                 |
|-------------------|----------------------------------------------------------------------------|
| Runtime           | .NET 11, C# 15 (`LangVersion` default)                                     |
| API               | ASP.NET Core Minimal API, Asp.Versioning                                   |
| Database          | PostgreSQL through EF Core (Npgsql provider, lazy loading proxies)         |
| Cache             | `HybridCache` (in-memory L1 + Redis L2)                                    |
| Authentication    | JWT bearer (Keycloak in development)                                       |
| Validation        | FluentValidation                                                           |
| Documentation     | OpenAPI + Scalar (development only)                                        |
| Observability     | OpenTelemetry, Serilog, health checks                                      |
| Dev orchestration | .NET Aspire (`AppHost`), used for local development only                   |
| Test runner       | Microsoft.Testing.Platform (set in `global.json`)                          |
| Tests             | xUnit v3, Shouldly, NSubstitute, Testcontainers, Respawn, coverlet.MTP     |

## 2. Commands

Run every command from the repository root unless stated otherwise.

```bash
dotnet restore
dotnet build MyApp.slnx
```

Run the tests (Docker is required, except for unit tests; see section 9.7 for the other commands):

```bash
dotnet test
dotnet test --filter-trait "Category=Unit"
```

Run the whole stack (API, PostgreSQL, Redis, Keycloak, MailPit) through Aspire:

```bash
dotnet run --project Src/AppHost
```

Run the API alone (the `Api` configuration section must be complete, see section 4):

```bash
dotnet run --project Src/Host/MyApp.Host.Api
```

Add an EF Core migration (generated in `Src/Domain/MyApp.Domain.Models/Migrations`). The tools start the API in
`Development`, so they read `appsettings.Development.json`; override with `Api__...` environment variables to target
another database:

```bash
dotnet ef migrations add <MigrationName> --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api --output-dir Migrations
```

Apply migrations to a database, or generate the SQL script to apply them elsewhere (both include the SQL scripts of
section 6.2):

```bash
dotnet ef database update --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api
```

```bash
dotnet ef migrations script --idempotent --project Src/Domain/MyApp.Domain.Models --startup-project Src/Host/MyApp.Host.Api --output migrations.sql
```

Rename the template (camelCase then PascalCase replacement of `myApp` / `MyApp`):

```bash
./setup.sh mywiki MyWiki
```

## 3. Architecture

### 3.1 Projects

```
Src/
├── AppHost/                          Aspire orchestration (development only)
├── ServiceDefaults/                  Shared host setup: logging, OpenTelemetry, health endpoints
├── Host/
│   └── MyApp.Host.Api/               HTTP layer: endpoints, API contracts, host configuration
├── Domain/
│   ├── MyApp.Domain.Models/          EF Core entities, AppDbContext, migrations, SQL scripts
│   └── MyApp.Domain.Specifications/  Reusable query predicates (specification pattern)
└── Services/
    ├── MyApp.Services.Abstractions/  Service contracts: interfaces, requests, responses
    ├── MyApp.Services/               Service implementations (use cases)
    └── MyApp.Services.Generators/    Roslyn source generator (pagination), netstandard2.0
Tests/                                Mirrors Src/ one-to-one (section 9)
├── MyApp.Tests.Shared/               Shared test infrastructure: categories, conventions, containers, database
├── Host/MyApp.Host.Api.Tests/
├── Domain/
│   ├── MyApp.Domain.Models.Tests/
│   └── MyApp.Domain.Specifications.Tests/
└── Services/
    ├── MyApp.Services.Tests/
    └── MyApp.Services.Generators.Tests/
```

`Domain/` holds the domain model (entities and their shared conditions). `Services/` holds the application layer:
the use cases built on top of the domain.

### 3.2 Dependency rules

```
Host.Api ──► Services ──► Services.Abstractions
   │            ├──────► Domain.Specifications ──► Domain.Models
   │            ├──────► Domain.Models
   │            └──────► Services.Generators (analyzer only)
   └──────► ServiceDefaults
AppHost ──► Host.Api (orchestration only)
```

- `Services.Abstractions` depends on nothing. It only contains contracts.
- `Domain.Specifications` only knows entities and expression trees. It never references EF Core APIs.
- `Services` does not know the database provider. Npgsql is only referenced by `Domain.Models`.
- `Host.Api` is the composition root. It is the only place that decides which infrastructure is used.
- The generator is referenced with `OutputItemType="Analyzer" ReferenceOutputAssembly="false"`.
- A test project references the project it tests (plus what its test models need) and `MyApp.Tests.Shared`.
  `MyApp.Tests.Shared` references no `Src/` project. `Src/` never references `Tests/`.

## 4. Configuration

The API declares its own typed configuration schema. Framework conventions such as the `ConnectionStrings` section
or Aspire's `WithReference` injection are not used.

### 4.1 Schema

Defined in `Src/Host/MyApp.Host.Api/ApiOption.cs`, bound from the `Api` section:

```
Api
├── Database:ConnectionString
├── Cache:ConnectionString
├── Auth:Domain
├── Auth:Audience
├── Cors:Origins[]
└── Services:DataPath          (bound to MyApp.Services.ServiceOption)
```

- `builder.GetAppConfig()` (`Extension/Configuration/ApiOptionConfiguration.cs`) binds the section and validates
  required values. It throws a single `InvalidOperationException` listing every missing key.
- Every configuration extension that needs settings calls `builder.GetAppConfig()`. Never read
  `builder.Configuration["..."]` directly.
- Environment variables use `__` as separator: `Api__Database__ConnectionString`, `Api__Cors__Origins__0`.

### 4.2 Adding a setting

1. Add the property (or a new `ApiXxxOption` sub-class) in `ApiOption.cs`.
2. If required, add a `Require(...)` line in `GetAppConfig()`.
3. Declare the key with an empty value in `Src/Host/MyApp.Host.Api/appsettings.json` (schema only, never real
   values) and put the local value in `appsettings.Development.json`.
4. Wire it from `Src/AppHost/Program.cs` with `.WithEnvironment("Api__Xxx__Yyy", ...)`, reading the value from the
   AppHost `api_config` section (`Src/AppHost/appsettings.Development.json`).

## 5. Composition and dependency injection

### 5.1 Host configuration extensions

Each concern of the host lives in `Src/Host/MyApp.Host.Api/Extension/Configuration/<Concern>Configuration.cs` and
exposes C# 14+ `extension` blocks:

- `AddAppXxx()` on `TBuilder where TBuilder : IHostApplicationBuilder`, returning `TBuilder`.
- `UseAppXxx()` on `WebApplication`, returning `WebApplication`.

Both return the same type they extend so `Program.cs` stays a single fluent chain. Framework methods that return
`IApplicationBuilder` (for example `UseRequestLocalization`) must be wrapped in a `UseAppXxx()` extension to keep the
chain typed.

Extensions that need settings read them themselves with `builder.GetAppConfig()` inside `AddAppXxx()`. Do not create
per-extension option classes nor pass settings from `Program.cs` through an `Action<T>`: every setting belongs to
`ApiOption` (section 4). When a `UseAppXxx()` needs a setting, resolve it during `AddAppXxx()` instead (for example
CORS registers a default policy with the configured origins, and `UseAppCors()` only calls `UseCors()`).

Middleware order in `Program.cs` matters: CORS, localization, exception handler, authentication, authorization,
OpenAPI, then endpoints.

### 5.2 Layer registration

Each layer exposes its own registration extension. The host composes them:

```csharp
builder.Services
    .AddMyAppPersistence(config.Database.ConnectionString)   // Domain.Models: DbContext pool, Npgsql
    .AddMyAppCache(config.Cache.ConnectionString)            // Services: HybridCache + Redis
    .AddMyAppServices(options => { ... });                   // Services: use cases, localization, options
```

A layer never registers infrastructure that belongs to another layer.

### 5.3 Automatic service registration

`AddMyAppServices()` scans the assemblies:

- every non-generic interface of `MyApp.Services.Abstractions` is registered as **scoped**;
- it must have **exactly one** implementing class in `MyApp.Services`, otherwise startup fails with an
  `InvalidOperationException` (none found, or several found);
- open generic interfaces (`IService<>`, `IServiceAsync<,>`, ...) are excluded.

Consequence: do not put an interface in `Services.Abstractions` unless it is a service implemented in `Services`.
No manual `AddScoped` is needed for a new service.

## 6. Recipes

### 6.1 Add an entity

1. Create `Src/Domain/MyApp.Domain.Models/Models/<Entity>.cs`.
2. Inherit `BaseEntity` (sequential `Guid` key) and implement the behaviour interfaces you need:
   - `ICreatedAt`: `CreatedAtUtc`, `UpdatedAtUtc`, `HasBeenModified`.
   - `ISoftDeletable`: `IsDeleted`, `DeletedAtUtc`, adds a named global query filter.
3. Configure it in an `internal sealed class <Entity>EntityConfiguration : IEntityTypeConfiguration<Entity>` in the
   same file, linked with `[EntityTypeConfiguration<<Entity>EntityConfiguration, <Entity>>]` on the entity.
4. In `Configure`, chain the shared builders, then the specific columns:

   ```csharp
   builder
       .ToTable("entity_name")
       .AddBaseEntity()
       .AddCreatedAt()
       .AddSoftDeletion();
   ```

   Table and column names are **snake_case**, set explicitly with `ToTable` and `HasColumnName`.
5. Add the `DbSet` in `AppDbContext`. Its name is the **plural of the entity class name**:
   `public virtual DbSet<Product> Products { get; set; }`.
6. Create the migration (section 2). Migrations live in the `Migrations` folder of `MyApp.Domain.Models`.
7. Use the helpers instead of setting fields by hand: `entity.SetCreatedAtData()`, `entity.SetUpdatedAtData()`,
   `entity.SetSoftDeletableData()`.

Property rules (nullable reference types are enabled):

| Property kind                        | Declaration                                                           |
|--------------------------------------|-----------------------------------------------------------------------|
| Column                               | `public required string Name { get; set; }`                           |
| Optional column                      | `public string? Description { get; set; }`                            |
| Required reference navigation        | `public virtual Category Category { get; set; } = null!;`             |
| Optional reference navigation        | `public virtual Category? Category { get; set; } = null;`             |
| Collection navigation                | `public virtual ICollection<Order> Orders { get; set; } = [];`        |

- Non-nullable columns are `required` so every creation site sets them explicitly.
- Nullable columns are not `required`: when they are not set, they are `null`.
- Navigation properties are always `virtual` (lazy loading proxies) and never `required`: they are initialized by
  default as shown above.
- Collections are always exposed as `ICollection<T>`. A model class never implements or inherits a collection
  (`class PostCollection : List<Post>`): models represent database tables, and such a class would be treated as a
  reference navigation by the pagination generator.
- Multidimensional arrays (`int[,]`) are not supported by the pagination generator. Binary columns (`byte[]`) and
  unsupported types (`JsonElement`, `Dictionary<,>`...) are simply ignored by filters and sorts.
- Properties coming from the shared behaviours (`Id`, `ICreatedAt`, `ISoftDeletable`) are not `required`: they are
  generated by EF Core or set by the helpers.
- Order inside the class: the entity's own properties first (columns, then navigations), then the properties coming
  from the implemented interfaces, separated from the rest by an empty line.

```csharp
[EntityTypeConfiguration<ProductEntityConfiguration, Product>]
public class Product : BaseEntity, ICreatedAt
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required Guid CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<Order> Orders { get; set; } = [];

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public bool HasBeenModified { get; set; }
}
```

### 6.2 SQL scripts around a migration

A migration can run raw SQL scripts before and after its generated operations. Put them in:

```
Src/Domain/MyApp.Domain.Models/SqlScripts/<MigrationId>/
├── Before/          executed before the migration operations
│   ├── *.sql
│   └── <any sub-folder>/*.sql
└── After/           executed after the migration operations
    ├── *.sql
    └── <any sub-folder>/*.sql
```

Example for the migration `20261118093000_AddProduct` (the id of its `[Migration]` attribute, which is also the name
of the file generated by `dotnet ef migrations add`):

```
SqlScripts/20261118093000_AddProduct/
└── After/
    ├── 01_product/
    │   ├── 01_seed.sql
    │   └── 02_ix_product_name.no-transaction.sql
    └── 02_user/
        └── seed.sql
```

Rules:

- `Before` and `After` are searched recursively, so scripts can be organized in any hierarchy.
- Inside a stage, scripts run in **ordinal order of their relative path**. Prefix folders and files with numbers
  (`01_`, `02_`) to control the order; do not rely on names alone (`product/` runs before `user/`, `10_` before `2_`).
- A script whose name ends with `.no-transaction.sql` runs outside of the migration transaction (needed for
  `CREATE INDEX CONCURRENTLY`, `VACUUM`...). EF Core logs a warning for it: if the process stops during that script,
  the migration stays partially applied.
- Batch separators such as SQL Server's `GO` are supported (handled by the database provider).
- Scripts are not reverted when a migration is rolled back (`Down`).

How it works (nothing to do by hand):

- `*.sql` files under `SqlScripts/` are embedded in the `MyApp.Domain.Models` assembly.
- Every migration inherits `SqlScriptMigration` (`SqlScriptMigrations/SqlScriptMigration.cs`), which injects the
  scripts as EF Core SQL operations around the generated ones. Scripts are therefore part of every way of applying
  migrations: `Database.MigrateAsync()`, `dotnet ef database update`, `dotnet ef migrations script` and bundles.
- `dotnet ef migrations add` generates migrations that already inherit `SqlScriptMigration`, thanks to
  `SqlScriptMigrationsGenerator`. It is registered for the EF Core tools by the `DesignTimeServicesReference`
  assembly attribute declared in `MyApp.Host.Api.csproj`. Never edit generated migration files to change their base
  class.

Migrations are applied automatically at startup (`Database.MigrateAsync()` in `Program.cs`) in the `Development`
environment only.

### 6.3 Add a specification

A specification is a reusable, composable predicate. Use it when the **same condition is needed by several
services** (for example "user has this id", "entity is active and owned by X"). It is not a repository: services keep
querying `AppDbContext` directly, specifications only factor out shared conditions.

1. Create `Src/Domain/MyApp.Domain.Specifications/<Entity>Specifications/<Entity><Condition>Spec.cs`:

   ```csharp
   public sealed class UserHasIdSpec : ISpecification<User>
   {
       private readonly Guid _id;

       public UserHasIdSpec(Guid id)
       {
           _id = id;
       }

       public Expression<Func<User, bool>> Criteria => x => x.Id == _id;
   }
   ```

2. Apply it with `query.Spec(new UserHasIdSpec(id))`.
3. Compose with `a.And(b)`, `a.Or(b)`, `a.Not()`. `a.And(b).Not()` negates the whole composition, `a.And(b.Not())`
   negates only `b`.

The predicate must stay translatable by EF Core: no method calls the provider cannot translate.

### 6.4 Add a service

One service is one use case. Naming: `<Entity><Action>` (`UserGetById`, `UserDeleteById`, `AuthSyncUser`).

1. Contract in `Src/Services/MyApp.Services.Abstractions/Services/<Domain>Services/I<Entity><Action>.cs`. Inherit
   one of the base interfaces of `IService.cs`:

   | Interface                         | Returns                                   |
   |-----------------------------------|-------------------------------------------|
   | `IServiceAsync<TQuery>`           | `Task<ServiceResponse>`                   |
   | `IServiceAsync<TQuery, TResult>`  | `Task<ServiceResponse<TResult>>`          |
   | `IService<TQuery>` / `<TQuery, TResult>` | synchronous equivalents            |

   When the input has several fields, declare it as a nested record named `Request`:

   ```csharp
   public interface IAuthSyncUser : IServiceAsync<IAuthSyncUser.Request>
   {
       public sealed record Request(string AuthId, string UserName, string Email);
   }
   ```

2. Implementation in `Src/Services/MyApp.Services/Services/<Domain>Services/<Entity><Action>.cs`:
   - `public sealed class`, constructor injection into `private readonly` fields;
   - inject `AppDbContext`, `HybridCache`, `IStringLocalizer<SharedResource>` as needed.
3. Return values use the `ServiceResponse` unions (`Services.Abstractions/ServiceResponse.cs`):
   - success without payload: `new ServiceSuccessResponse()`;
   - success with payload: the `TResult` value itself (implicit conversion);
   - error: `new ServiceErrorResponse(<httpStatusCode>, _localizer.GetString(ServiceConstant.Error.<key>))`.
4. Output types follow the same logic as inputs:
   - an output specific to one service is declared as a nested record named `Response` in its interface:

     ```csharp
     public interface IProductGetStock : IServiceAsync<Guid, IProductGetStock.Response>
     {
         public sealed record Response(Guid ProductId, int Quantity);
     }
     ```

   - an output shared by several services (typically `Get` and `GetById`) goes in
     `Services.Abstractions/Responses/<Entity>ServiceResponse.cs`.
5. Registration is automatic (section 5.3).

### 6.5 Add a paginated service

1. Mark the implementation with `[PaginationHandlerFor<TEntity>]`. The generator then produces
   `IQueryable<TEntity>.ProcessPaginationRequest(...)` (class `{Entity}PaginationExtensions`, namespace
   `MyApp.Services`) handling filtering, sorting and paging. Nothing else to declare: new entity properties and
   navigations are picked up at the next build.
2. Use it in the service:

   ```csharp
   return await _db.Users
       .AsNoTracking()
       .ProcessPaginationRequest(query.PaginationService, out var countAsync)
       .Select(UserProjection.ToUserServiceResponse)
       .ToPagedResponseAsync(query.PaginationService.PageNumber, query.PaginationService.PageSize, countAsync, ct);
   ```

3. The contract returns `PaginationServiceResponse<TResponse>` and takes a `Request` record wrapping a
   `PaginationServiceRequest`.

What the generated code supports:

| Feature            | Behaviour                                                                                          |
|--------------------|----------------------------------------------------------------------------------------------------|
| Property paths     | Dotted and case-insensitive, `_` ignored: `userName`, `UserName`, `user_name`, `role.name`. A property named after a C# keyword keeps its `@`: `@event`. |
| Inherited members  | Public readable properties of the class and of its base classes (`Id`, `CreatedAtUtc`...). A property implementing an interface is included (it is declared on the class); explicit interface implementations are not. |
| Filter types       | `string`, `bool`, `int`, `long`, `decimal`, `double`, enums, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, and their nullable versions. |
| Reference filters  | `role.name` becomes a join on the navigation.                                                      |
| Collection filters | `posts.likes` becomes `EXISTS (...)` (at least one item matches); collections can be nested (`posts.tags.label`). |
| Collection quantifiers | `posts.$none.title` (no item matches, `NOT EXISTS`) and `posts.$all.likes` (every item matches, true on an empty collection). Without quantifier, at least one item must match. Quantifiers can be nested: `posts.$all.tags.$none.label`. |
| Collection count   | `posts.$count` filters on the number of items (`COUNT` subquery), with the numeric operators: `posts.$count = 0` (no post), `posts.tags.$count > 2`. |
| Collections of values | `List<string>`, `DateOnly[]`... of a filter type: the item is the value, so the path ends with the collection or its `$` segment: `tags = "ef"` (at least one), `tags.$none`, `tags.$all`, `tags.$count`. Operators are the ones of the element type. `byte[]` is a binary column and is ignored. |
| Navigation depth   | At most 4 navigations in a filter path; going through a collection (and using `$count`, `$none`, `$all` on it) counts as one. Cycles (`posts.author.role.name`) are allowed. |
| Sorting            | Properties of a filter type only (the type tables below), through reference navigations up to 3 levels (`role.name`), self references included (`manager.manager.name`). Never through collections. Null values (missing references included) follow PostgreSQL: last when ascending, first when descending. |
| Null values        | Only a JSON `null` targets `NULL`: `Equal` becomes `IS NULL`, `NotEqual` `IS NOT NULL`, other operators are rejected. `""` and `"null"` are ordinary values: literal strings on a `string` property, unparsable (400) on the other types. A null value on a non-nullable property is rejected, except through an optional reference: a missing reference counts as `NULL`, so `mentor.rank = null` matches rows without mentor, and `country = null`, `country.name = null` and `countryId = null` return the same rows. |
| Negative operators | `NotEqual` and `NotContains` with a value assume the property is not null: a `null` property does not match (like SQL `<>`), including inside `$all` / `$none`. |
| Values             | Parsed with the invariant culture, dates converted to UTC, sent as SQL parameters.                  |
| Enums              | Matched by member name, case-sensitive (`Archived`, not `archived`). Numbers (`1`) and undefined names are unparsable values (400). |
| Errors             | Unknown path, unsupported operator or unparsable value throws `InvalidPaginationRequestException`, returned as `400` by the API. |
| Name collisions    | Two properties with the same normalized name (`UserName` / `User_Name`): only the first is usable, warning `PAG001` at build time. Two paginated entities with the same name in different namespaces: the second extensions class is suffixed (`User2PaginationExtensions`), info `PAG002`. |

Operators by type:

| Type                                 | Operators                                                                 |
|--------------------------------------|---------------------------------------------------------------------------|
| `string`                             | `Equal`, `NotEqual`, `Contains`, `NotContains`, `StartWith`, `EndWith` (text operators are case-insensitive) |
| Numbers, dates                       | `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` |
| `bool`, enums, `Guid`                | `Equal`, `NotEqual`                                                       |

Navigations are only followed into classes declared in the same assembly as the root entity (`MyApp.Domain.Models`),
so framework types are never walked. Without sort request, the order of the rows is the database one. Clients should
end their sorts with a unique key (`id`): rows with equal sort values have no guaranteed order, so they can move
between pages. This is the client's choice, the API does not add it.

How the generator is organized (`Src/Services/MyApp.Services.Generators/`):

- `PaginationGenerator.cs`: finds the `[PaginationHandlerFor<TEntity>]` roots, walks the navigation graph and emits
  one `{Entity}FilterBuilder` per reachable entity (internal, namespace `MyApp.Services.Generated.Filtering`), the
  shared `FilterExpressionHelper`, and the `{Entity}PaginationExtensions` of each root. The file starts with a
  reading guide and a walkthrough of one filter down to the SQL.
- `Parser/ScalarFilterEmitter.cs`: emits the parsing and comparison code for each scalar type (and for `$count`).
  Add a type there to make it filterable and sortable (see below).
- `AnalyzerReleases.*.md`: release tracking of the generator diagnostics (`PAG001`, `PAG002`).

Known limits of the generated code:

- Without quantifier, inverting the operator does not invert the meaning: `posts.title != "x"` means "at least one
  post whose title is not x". Use `posts.$none.title = "x"` for "no post titled x".
- `$all` is true on an empty collection: combine it with `posts.$count > 0` to require at least one item.
- `$` segments are reserved and only valid right after a collection (`role.$none.name` is rejected). `$count` ends the
  path; on a collection of entities, `$none` / `$all` must be followed by an item property.
- A filter takes a single value: no `In [a, b, c]` (combine `Equal` filters with `Or`).
- Logic is evaluated left to right without precedence: `A OR B AND C` is `(A OR B) AND C`. Use nested `filters` for
  another grouping: a nested group is evaluated first, whatever its position, so `[X, A (Or) { B (And) }]` is
  `X OR (A AND B)`.
- String `Equal` / `NotEqual` are case-sensitive, text operators are not. The `lower()` calls of text operators
  prevent the use of regular indexes on large tables.
- Every public property of a supported type (and every navigation) of the entity and of every reachable entity can be
  used in filters, and the scalar ones in sorts, sensitive ones included (`AuthId`, `Email`...). Filters only build
  parameterized queries (no SQL injection is possible), but a client can infer values that the response does not
  return by filtering on them. This is accepted by design. Property names are the entity ones, not the API response
  ones.
- Types not listed above (`short`, `float`, `TimeSpan`, `TimeOnly`, custom structs...) are neither filterable nor
  sortable until they are added to `ScalarFilterEmitter`.

#### Adding a filterable type

Every filterable type is described in `ScalarFilterEmitter.TryGetInfo` by a `ScalarInfo`:

| Argument     | Meaning                                                                                          |
|--------------|--------------------------------------------------------------------------------------------------|
| `typeName`   | Fully qualified, non-nullable type name (use the `name` variable).                              |
| `isNullable` | Pass the `isNullable` variable: `Nullable<T>` is unwrapped before, and gets `IS NULL` support for free. |
| `operators`  | `OperatorSet.Equatable` (equal, not equal), `Comparable` (+ ordering) or `String` (+ text operators). |
| `parse`      | C# boolean expression parsing `request.Value` into `out var parsed`. Use the invariant culture.  |
| `converted`  | Expression turning `parsed` into the compared value (usually `"parsed"`, `"parsed.ToUniversalTime()"` for dates). |

Once declared, the type is filterable and sortable everywhere: nullable version, through navigations, and as the
element of a collection of values (`List<TimeOnly>`, `TimeOnly[]`...). The generated code picks it up at the next
build. Update the type tables of this section and of the README.

1. **Type with a `SpecialType`** (most C# keyword types: `short`, `byte`, `float`, `uint`...): add it to the matching
   `case` of the `switch (type.SpecialType)`. Example for `short` and `float`, which parse like the other numbers:

   ```csharp
   case SpecialType.System_Int16:
   case SpecialType.System_Single:
   case SpecialType.System_Int32:
   // ...
       info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
           $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
       return true;
   ```

2. **Framework type without `SpecialType`** (`TimeOnly`, `TimeSpan`...): add a `case` on its fully qualified name in
   the `switch (name)`:

   ```csharp
   case "global::System.TimeOnly":
       info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
           $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
       return true;
   ```

3. **Custom type** (strongly typed id, value object `struct`...): same as 2 with its own name and parsing, for
   example a `record struct UserId(Guid Value)` implementing `IParsable<UserId>`:

   ```csharp
   case "global::MyApp.Domain.Models.Models.UserId":
       info = new ScalarInfo(name, isNullable, OperatorSet.Equatable,
           $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
       return true;
   ```

   Constraints for custom types:
   - the comparison is built with `Expression.Equal` / `Expression.GreaterThan`..., so the type must define the
     matching operators (`==`, `!=`, and `<`, `>`... for `Comparable`); records define `==` / `!=` automatically;
   - EF Core must be able to translate the comparison: the type needs a value converter in its entity configuration,
     and ordering operators compare the **converted** database value, so only use `Comparable` when that order is
     meaningful;
   - supported scalars are checked before navigations, so a type declared in `TryGetInfo` is always handled as a
     scalar. Without that declaration, a class of `MyApp.Domain.Models` is treated as a navigation (prefer `struct`s
     for value objects meant to be filtered as a single value).

Always check the translation against PostgreSQL after adding a type (for example `uint` has no native PostgreSQL
type).

To inspect the generated code, build with
`dotnet build Src/Services/MyApp.Services --no-incremental -p:EmitCompilerGeneratedFiles=true` and look under
`Src/Services/MyApp.Services/obj/Debug/net11.0/generated/`.

### 6.6 Projections

Projections live in `Src/Services/MyApp.Services/ServiceProjections/<Entity>Projection.cs`, as pairs:

```csharp
internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user => new ... { };
internal static readonly Func<User, UserServiceResponse> ToUserServiceResponseFunc = ToUserServiceResponse.Compile();
```

- Use the `Expression` in `IQueryable.Select(...)` so EF Core only selects the needed columns.
- Use the compiled `Func` on in-memory objects.

### 6.7 Add an error message

1. Add the key in `ServiceConstant.Error` (`public const string my_error = nameof(my_error);`, snake_case).
2. Add the translation in `Src/Services/MyApp.Services/Resources/SharedResource.resx` (English) and
   `SharedResource.fr.resx` (French).
3. Return it with `_localizer.GetString(ServiceConstant.Error.my_error)`.

Supported cultures: `en` (default) and `fr`.

### 6.8 Cache

- Inject `HybridCache` directly (no custom cache abstraction).
- Keys and tags are defined in `ServiceConstant.Cache` (`UserKey(id)`, `UserTag`).
- Read with `GetOrCreateAsync`, write with `SetAsync`, invalidate with `RemoveAsync` or `RemoveByTagAsync`.
- Never cache EF entities. Cache a dedicated model from `Src/Services/MyApp.Services/CacheModels/` built with a
  projection (`UserProjection.ToCacheUser`).
- A given key always stores the same type, whichever service writes it.
- `GetOrCreateAsync` also caches a `null` result. Make sure the service that creates the data overwrites the key.
- Accepted for now: a change made outside of the services (directly in the database) stays invisible until the entry
  expires, and another API instance can serve its in-memory (L1) copy of an invalidated entry until it expires. Both
  are planned to be solved by a cache rework (FusionCache backplane), which will also revisit the caching of `null`.

### 6.9 Add an endpoint

1. If the feature is a new area, add an API builder in `Endpoints/ApiBuilder.cs`. Always create the versioned API
   with `NewAppVersionedApi(...)`, never `NewVersionedApi(...)` directly: it adds the conventions shared by every
   endpoint, such as automatic validation.

   ```csharp
   public sealed class ProductApiBuilder(IVersionedEndpointRouteBuilder builder) : VersionedApiBuilder(builder);

   public static ProductApiBuilder MapProduct(this IEndpointRouteBuilder builder)
   {
       return new ProductApiBuilder(builder.NewAppVersionedApi("Product").RequireAuthorization());
   }
   ```

2. Create `Endpoints/<Area>Endpoint.cs` with a `ToV1()` extension declaring routes on a versioned group, and the
   handlers in a `private static class V1`:

   ```csharp
   var api = builder.MapGroup("/products").HasApiVersion(1.0);

   api.MapGet("{id}", V1.GetById)
       .Produces<ProductApiResponse>()
       .Produces<ProblemHttpResult>(StatusCodes.Status404NotFound);
   ```

3. A handler injects the service with `[FromServices]`, calls it, then maps the union with a `switch`:

   ```csharp
   return result switch
   {
       ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
       ProductServiceResponse response => TypedResults.Ok(new ProductApiResponse(response))
   };
   ```

4. Register it in `UseAppEndpoints()` (`ApiEndpointConfiguration.cs`): `app.MapProduct().ToV1();`.
5. Declare every possible status code with `.Produces...` so the OpenAPI document is accurate.
6. Paginated lists use the `QUERY` HTTP method with the pagination request in the body:
   `api.MapMethods(string.Empty, ["QUERY"], V1.Get)`.
7. Admin-only routes add `.RequireAuthorization(ApiConstant.AuthorizationPolicies.Admin)`.

A new version adds a `ToV2()` and a `V2` class next to the existing ones.

### 6.10 API contracts

API contracts are separate from service contracts and live in `Host.Api`:

- `Requests/<Name>ApiRequest.cs`: `sealed record` with a `ToServiceRequest()` method converting it to the service
  request. Its FluentValidation validator is a nested `sealed class Validator : AbstractValidator<...>`.
- Validation is automatic: `AddAppValidation()` registers every validator of the API assembly, and `ValidationFilter`
  (added by `NewAppVersionedApi`) validates every handler argument that has a validator before the handler runs.
  Invalid input returns a `400` validation problem (`EndpointHelper.FormatValidationErrorResponse`). Never inject
  `IValidator<T>` nor call `Validate` in a handler.
- `Responses/<Name>ApiResponse.cs`: `sealed record` with a constructor taking the service response.

Services never return API types and endpoints never expose service types.

## 7. Cross-cutting concerns

- **Authentication**: JWT bearer, authority and audience from `Api:Auth`. HTTPS metadata is only disabled in
  development. Current user data is read through `ClaimsPrincipalExtension` (`user.CurrentId`,
  `user.CurrentUserName` from the `preferred_username` claim, `user.CurrentEmail`). A token missing one of them is an
  invalid request (`400`).
- **User synchronization**: the client calls `POST /auth/sync-user` at each sign-in. It only inserts the user the
  first time and never updates an existing one (this may change with the BFF). Two concurrent first syncs: the loser
  gets `409`. A soft-deleted user gets `403`, on which the client signs out; `sync-user` is the only route returning
  it for now.
- **Users**: every authenticated user can list users and read any of them (rights are deliberately minimal, so that
  projects built on the template add their own). Admins delete users, never their own account (`400`); deleting an
  already deleted user returns `400` (`user_already_deleted`).
- **Authorization**: policies are declared in `AuthorizationConfiguration`, names and roles in `ApiConstant`.
- **Antiforgery**: header `X-XSRF-TOKEN`, cookie `XSRF-TOKEN`, token issued by `GET /misc/anti-forgery-token`.
  Routes do not validate it yet: the BFF will handle it.
- **CORS**: default policy registered in `AddAppCors()`, allowed origins from `Api:Cors:Origins`, credentials
  allowed, `Content-Disposition` exposed.
- **Errors**: unhandled exceptions are logged and returned as `500`. An unparsable route value (a non-GUID `id`) is a `400`
  problem. Validation errors are returned as `400`
  validation problems (section 6.10), invalid pagination requests (`InvalidPaginationRequestException`) as `400`
  problems (section 6.5). Business errors go through
  `ServiceErrorResponse` and are returned as RFC 7807 problem details by `EndpointHelper.FormatErrorResponse`.
- **Soft deletion**: entities implementing `ISoftDeletable` are filtered by the named filter
  `ModelConstant.SoftDeletionFilter`. To read deleted rows, use
  `.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])` and explain why in a comment.
- **Versioning**: read from the query string or the `X-Api-Version` header, default `1.0`.
- **OpenAPI / Scalar**: mapped in development only, one document per API version, UI at `/scalar`.
- **Health checks**: mapped in every environment by `MapDefaultEndpoint`. `/health` runs every check (disk storage,
  PostgreSQL, Redis) and returns a detailed JSON report; `/alive` only runs the checks tagged
  `ServiceDefaultConstant.LiveTag` (the `self` check), for liveness probes.
- **File storage**: `IFileStorageService` works with file names relative to `Api:Services:DataPath`; every method
  takes and returns such names. Names resolving outside of that folder (absolute paths, `..`) are rejected with an
  `ArgumentException`.
- **Logging**: Serilog (console, file, OpenTelemetry sink), OTLP export when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

## 8. Code conventions

- File-scoped namespaces matching the folder path. Nullable reference types and implicit usings are enabled.
- Classes are `sealed` unless designed for inheritance. Entities are not sealed (lazy loading proxies).
- Services and specifications use explicit constructors with `private readonly` fields.
- Use the C# 14 `extension` block syntax for new extension methods.
- Results are modelled with C# 15 unions (`ServiceResponse`) and consumed with exhaustive `switch` expressions.
- Prefer pattern matching (including list patterns) over chains of `if` when it reads better.
- Naming suffixes:

  | Suffix                    | Meaning                                  | Project                  |
  |---------------------------|------------------------------------------|--------------------------|
  | `ApiRequest` / `ApiResponse` | HTTP contracts                        | Host.Api                 |
  | `ServiceRequest` / `ServiceResponse` | service contracts             | Services.Abstractions    |
  | `Spec`                    | specification                            | Domain.Specifications    |
  | `Projection`              | expression projections                   | Services                 |
  | `Configuration`           | host configuration extensions            | Host.Api                 |
  | `Option`                  | configuration schema classes             | Host.Api, Services       |
  | `Endpoint`                | endpoint group                           | Host.Api                 |

- Constants are grouped in a static `<Layer>Constant` class (`ApiConstant`, `ServiceConstant`, `ModelConstant`).
- Remove unused `using` directives.

## 9. Tests

### 9.1 Stack and layout

| Concern              | Technology                                                                        |
|----------------------|-----------------------------------------------------------------------------------|
| Framework            | xUnit v3 on Microsoft.Testing.Platform v2, run by `dotnet test` (`global.json`)   |
| Assertions, doubles  | Shouldly, NSubstitute                                                             |
| Infrastructure       | Testcontainers (PostgreSQL, Redis), Respawn, `WebApplicationFactory`              |
| Results, coverage    | TRX (`Microsoft.Testing.Extensions.TrxReport`), coverlet.MTP (Cobertura, OpenCover) |

- `Tests/` mirrors `Src/` one-to-one, grouping folders included (section 3.1). There is no test project for
  `Services.Abstractions` (contracts only), `ServiceDefaults` and `AppHost`. Test projects are in the `/Tests/...`
  solution folders of `MyApp.slnx`.
- `Tests/Directory.Build.props` applies to every project of `Tests/`. A project whose name ends with `.Tests` gets the
  test packages, the reference to `MyApp.Tests.Shared`, the global usings (`Xunit`, `Shouldly`, the categories) and
  `Tests/testconfig.json`. Its own `.csproj` only references what it tests.
- Inside a project, folders mirror the source folder of the tested class
  (`Services/UserServices/UserGetById.Integration.cs` tests `Services/UserServices/UserGetById.cs`). There are no
  `Unit/`, `Integration/` or `Functional/` folders: all the tests of a class stay together.
- Exceptions: endpoint functional tests are in `Endpoints/<Area>Endpoint.Functional.cs`; generator tests are grouped
  by behaviour (`Filters/`, `Sorts/`, `Paging/`, `Errors/`, `Diagnostics/`) on the entities of `TestModels/`.
- Test-only code lives in `TestInfrastructure/` (base classes, fixtures, helpers) and `TestModels/`,
  `TestMigrations/`, `TestSpecifications/` (test entities, contexts, migrations, specifications).

### 9.2 Conventions

- One `public sealed partial class <Class>Tests` per tested class, one file per kind of test: `<Class>.Unit.cs`,
  `<Class>.Integration.cs`, `<Class>.Functional.cs`. Each file holds a nested `public sealed class` named `Unit`,
  `Integration` or `Functional`.
- The nested class carries the xUnit category trait built from the `TestCategory` constants
  (`MyApp.Tests.Shared.Categories`): `[Trait(TestCategory.Name, TestCategory.Unit)]`, `TestCategory.Integration` or
  `TestCategory.Functional`. Never write the strings by hand. The built-in `[Trait]` is used on purpose instead of
  custom attributes: IDEs (Rider) read categories from the source and only understand `[Trait]`.
- Every project has a convention test (`Conventions/TestConvention.Unit.cs`, calling
  `assembly.ShouldFollowTestConventions()`). It fails when a nested class name and its category trait do not match, when a
  category trait is used elsewhere (assembly, method, other class), or when a class declares tests without category.
- Test names: `Method_Context_ExpectedResult` (`ExecuteAsync_UnknownId_Returns404`,
  `Query_AsUser_ReturnsRequestedPage`). Functional tests use the HTTP method as `Method`.
- Pass `TestContext.Current.CancellationToken` (the `Ct` property of the base classes) to async calls.
- A known bug is documented by a skipped test asserting the expected behaviour: `[Fact(Skip = "Bug: <what happens>")]`.
  Fix the bug, then remove the `Skip`.

### 9.3 Levels

| Level       | Trait                  | What is real                                                                                  | Docker |
|-------------|------------------------|-----------------------------------------------------------------------------------------------|--------|
| Unit        | `Category=Unit`        | The tested class only, in memory: specifications, validators, `GetAppConfig()`, DI composition, `FileStorageService` on a temporary folder, generator diagnostics (`CSharpGeneratorDriver`). | no |
| Integration | `Category=Integration` | The real layer on PostgreSQL and Redis containers: the host composition (`AddMyAppPersistence`, `AddMyAppCache`, `AddMyAppServices`), the generated pagination code, migrations and SQL scripts. | yes |
| Functional  | `Category=Functional`  | The whole API (`Program`) over HTTP with `WebApplicationFactory`, on the containers. Only authentication is replaced (section 9.5). | yes |

Service and generator tests never depend on authentication.

### 9.4 Shared infrastructure

`MyApp.Tests.Shared` (a library, not a test project):

- `ContainersFixture`: PostgreSQL and Redis (same images as the AppHost), one pair per test assembly. A project
  declares it in `AssemblyInfo.cs` with `[assembly: AssemblyFixture(typeof(ContainersFixture))]` and tests receive it
  through their constructor. The containers start lazily: a test calls `await containers.EnsureStartedAsync()` (the
  base classes do it in `InitializeAsync`), so a unit-only run never needs Docker.
- `DatabaseSchema.CreateAsync(context)`: applies the migrations (SQL scripts included) when the model has some,
  otherwise creates the schema from the model (the template ships without migrations).
- `DatabaseReset`: empties every table except the migration history (Respawn). The base classes call it before each
  test.
- `TemporaryDatabase`: an empty database created on the container and dropped on dispose, for tests that apply
  migrations.

Each project has a `TestInfrastructure/DatabaseCollection` (`[Collection(DatabaseCollection.Name)]`, name
`"Database"`): the integration and functional classes share the container database, so they join this collection and
run one after the other. Unit classes stay outside of it and run in parallel.

Base classes: `ServiceIntegrationTest` (real composition, one service provider per test, `CreateProvider()` for a
second API instance), `PaginationIntegrationTest` (test database seeded once per run: read only, never write to it),
`ApiFunctionalTest` (API host, `CreateClient()`, `SeedUserAsync`, problem readers).

### 9.5 API functional tests and authentication

- `ApiFactory` (`WebApplicationFactory<Program>`) runs the API in the `Testing` environment: not `Development`, so
  neither `appsettings.Development.json` nor the startup migration are used. Every required `Api:*` setting is given
  with `UseSetting`, and the schema is created once. It is the fixture of the `Database` collection and starts lazily.
- JWT bearer is replaced by `TestAuthHandler`, which produces the claims read by `ClaimsPrincipalExtension`.
- Tests only express who calls, with `ApiClientAuthExtensions`: clients are anonymous by default (`AsAnonymous()`),
  `AsUser(...)`, `AsAdmin(...)` or `As(TestUser)`. `await client.WithAntiforgeryTokenAsync()` sends the antiforgery
  token like a SPA. Other request options are in `ApiClientExtensions` (`WithCulture`, `WithApiVersionHeader`,
  `QueryAsJsonAsync`).
- No test builds tokens, cookies, claims or authentication headers itself. When authentication moves behind the BFF,
  only `ApiFactory`, `TestAuthHandler` and `ApiClientAuthExtensions` change, never the test bodies.

### 9.6 Add a test

1. Open the test project mirroring the source project, and the folder mirroring the source folder of the class.
2. Add the test to `<Class>.<Kind>.cs`, creating the file with the partial class and the nested class if needed:

   ```csharp
   public sealed partial class UserGetByIdTests
   {
       [Trait(TestCategory.Name, TestCategory.Integration)]
       [Collection(DatabaseCollection.Name)]
       public sealed class Integration(ContainersFixture containers) : ServiceIntegrationTest(containers)
       {
           [Fact]
           public async Task ExecuteAsync_UnknownId_Returns404()
           {
               var result = await RunAsync<IUserGetById, ServiceResponse<UserServiceResponse>>(
                   x => x.ExecuteAsync(Guid.NewGuid(), Ct));

               result.Value.ShouldBeOfType<ServiceErrorResponse>().StatusCode.ShouldBe(404);
           }
       }
   }
   ```

3. A unit class has no fixture and no collection. An integration or functional class inherits the base class of its
   project (or calls `EnsureStartedAsync`) and joins `[Collection(DatabaseCollection.Name)]`.
4. A new test project is named `<SourceProject>.Tests`, placed in the mirrored folder and added to `MyApp.slnx` under
   the matching `/Tests/...` folder. It references the tested project, copies `Conventions/TestConvention.Unit.cs`,
   and declares the `ContainersFixture` in `AssemblyInfo.cs` if it needs containers.

### 9.7 Commands

```bash
dotnet test                                                  # every test (Docker required)
dotnet test --filter-trait "Category=Unit"                   # unit tests only, no Docker
dotnet test --project Tests/Services/MyApp.Services.Tests    # one project
dotnet test --coverlet --report-trx                          # with coverage and TRX, like the CI
```

Results land in `TestResults/` at the repository root (git-ignored): one `<Project>_net11.0_<arch>.trx` and one
`coverage.cobertura.<timestamp>.xml` / `coverage.opencover.<timestamp>.xml` pair per test project. File names are
timestamped: use globs. Coverage settings (included assemblies, exclusions, formats) are in `Tests/testconfig.json`.

## 10. Git and CI

- Commit messages follow Conventional Commits: `feat:`, `fix:`, `refactor:`, `perf:`, `chore:`, optional scope
  (`refactor(generator): ...`).
- Work on a branch, merge into `main` through a pull request.
- `.github/workflows/pull_request.yml`, on every pull request and push to `main`:
  - job `test`: restore, build, `dotnet test --no-build --coverlet --report-trx` (Testcontainers uses the Docker
    engine of the runner), then uploads the `test-results` (TRX) and `coverage` (Cobertura, OpenCover) artifacts,
    also when tests fail;
  - job `codecov`: opt-in, calls `reusable_codecov.yml` after `test` (see below).
- `.github/workflows/reusable_codecov.yml` (`on: workflow_call`): downloads the `coverage` artifact and uploads the
  Cobertura files with `codecov/codecov-action`, authenticated with OIDC.
- `.github/workflows/create_docker_image.yml`: on a published release, builds `docker/myApp/Dockerfile` for
  `linux/amd64` and `linux/arm64` and pushes it to `ghcr.io`.
- GitHub forbids sub-folders in `.github/workflows`: reusable workflows are prefixed `reusable_` and named
  `"Reusable - <Name>"`. File names are snake_case.
- Permissions: `contents: read` at workflow level; a job needing more declares it itself (`id-token: write` on the
  `codecov` job only).
- What the CI produces: the job status (exit code of the test platform) is the only built-in signal, and the
  artifacts are on the run summary page. The template posts nothing on pull requests: integrations are provided,
  never imposed.
- Codecov is the only reporting integration, disabled by default. To enable it: link the repository on codecov.io
  (Codecov GitHub app), then set the repository variable `CODECOV_ENABLED` to `true` (Settings > Secrets and variables
  > Actions > Variables). No token is stored: the upload uses OIDC. Codecov then posts its own comment and checks.
- Test results are not sent to Codecov: Test Analytics only accepts JUnit XML, not TRX. Once
  `Microsoft.Testing.Extensions.JUnitReport` is stable, add `--report-junit` and a second upload with
  `report_type: test_results` (see the comment in `reusable_codecov.yml`).

## 11. Known pitfalls

- Migrations and SQL scripts run automatically in `Development` only. Other environments must apply them explicitly
  (`dotnet ef database update`, generated SQL script or bundle, see section 2).
- A SQL script placed in a folder that does not match the exact migration id (`[Migration]` attribute, timestamp
  included) is silently ignored.
- Statements forbidden inside a transaction (`CREATE INDEX CONCURRENTLY`, `VACUUM`...) fail unless the script name
  ends with `.no-transaction.sql`.
- Script order is ordinal on the relative path: without numeric prefixes, `10_x.sql` runs before `2_x.sql`.
- Adding an interface to `Services.Abstractions` without exactly one implementation in `Services` makes startup fail
  (section 5.3).
- Missing required configuration makes startup fail with the list of missing `Api:...` keys (section 4).
- `dotnet test` fails with exit code `8` when a filter selects no test in a project (for example
  `--filter-trait "Category=Integration"` on `MyApp.Domain.Specifications.Tests`, which only has unit tests). Every
  project has a unit convention test, so `Category=Unit` always matches; for other filters target the projects that
  have such tests, or add `--ignore-exit-code 8`.
- Integration and functional tests fail at `EnsureStartedAsync` when Docker is not running.
- Coverage: the `include` of `Tests/testconfig.json` (`[MyApp.*]*`) works around coverlet issue #2071, where the
  excludes computed from the `deps.json` can drop the tested assemblies. Keep it explicit (`setup.sh` renames it).
- Skipped tests whose reason starts with `Bug:` document known bugs, they are not flaky tests: do not delete them.
- A test class declaring tests outside of a nested `Unit` / `Integration` / `Functional` class fails the convention
  test of its project.

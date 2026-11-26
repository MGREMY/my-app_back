# Pagination generator

> **Read this when** you add a paginated list, change an entity that is paginated, make a new type filterable, or
> work on `MyApp.Services.Generators`.

Paginated lists accept filters, sorts and paging from the client. Instead of writing that code by hand for each
entity, a Roslyn source generator (`MyApp.Services.Generators`, netstandard2.0) produces it at build time from the
entity classes. This page covers, in order: how to use it, what it supports, its known limits, and how it is built.

The client-side view (request format, property names, examples) is in the [README](../../README.md#pagination-filtering-and-sorting).

## Adding a paginated service

1. Mark the service implementation with `[PaginationHandlerFor<TEntity>]`. The generator then produces
   `IQueryable<TEntity>.ProcessPaginationRequest(...)` (class `{Entity}PaginationExtensions`, namespace
   `MyApp.Services`), which handles filtering, sorting and paging. Nothing else to declare: new entity properties and
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

On the API side, paginated lists use the `QUERY` HTTP method (see [Endpoints](endpoints.md#adding-an-endpoint)).

## What the generated code supports

### Paths

| Feature           | Behaviour |
|-------------------|-----------|
| Property paths    | Dotted and case-insensitive, `_` ignored: `userName`, `UserName`, `user_name`, `role.name`. A property named after a C# keyword keeps its `@`: `@event`. |
| Inherited members | Public readable properties of the class and of its base classes (`Id`, `CreatedAtUtc`...). A property implementing an interface is included (it is declared on the class); explicit interface implementations are not. |
| Navigation depth  | At most 4 navigations in a filter path. Going through a collection (and using `$count`, `$none`, `$all` on it) counts as one. Cycles (`posts.author.role.name`) are allowed. |

Navigations are only followed into classes declared in the same assembly as the root entity (`MyApp.Domain.Models`),
so framework types are never walked.

### Filters

| Feature                | Behaviour |
|------------------------|-----------|
| Filter types           | `string`, `bool`, `int`, `long`, `decimal`, `double`, enums, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, and their nullable versions. |
| Reference filters      | `role.name` becomes a join on the navigation. |
| Collection filters     | `posts.likes` becomes `EXISTS (...)`: at least one item matches. Collections can be nested (`posts.tags.label`). |
| Collection quantifiers | `posts.$none.title` (no item matches, `NOT EXISTS`) and `posts.$all.likes` (every item matches, true on an empty collection). Without quantifier, at least one item must match. Quantifiers can be nested: `posts.$all.tags.$none.label`. |
| Collection count       | `posts.$count` filters on the number of items (`COUNT` subquery), with the numeric operators: `posts.$count = 0` (no post), `posts.tags.$count > 2`. |
| Collections of values  | `List<string>`, `DateOnly[]`... of a filter type: the item is the value, so the path ends with the collection or its `$` segment: `tags = "ef"` (at least one), `tags.$none`, `tags.$all`, `tags.$count`. Operators are the ones of the element type. `byte[]` is a binary column and is ignored. |

### Operators by type

| Type                  | Operators |
|-----------------------|-----------|
| `string`              | `Equal`, `NotEqual`, `Contains`, `NotContains`, `StartWith`, `EndWith` (text operators are case-insensitive) |
| Numbers, dates        | `Equal`, `NotEqual`, `GreaterThan`, `GreaterThanOrEqual`, `LessThan`, `LessThanOrEqual` |
| `bool`, enums, `Guid` | `Equal`, `NotEqual` |

### Values

| Feature            | Behaviour |
|--------------------|-----------|
| Parsing            | Invariant culture, dates converted to UTC, always sent as SQL parameters. |
| Enums              | Matched by member name, case-sensitive (`Archived`, not `archived`). Numbers (`1`) and undefined names are unparsable values (400). |
| Null values        | Only a JSON `null` targets `NULL`: `Equal` becomes `IS NULL`, `NotEqual` `IS NOT NULL`, other operators are rejected. `""` and `"null"` are ordinary values: literal strings on a `string` property, unparsable (400) on the other types. A null value on a non-nullable property is rejected, except through an optional reference: a missing reference counts as `NULL`, so `mentor.rank = null` matches rows without mentor, and `country = null`, `country.name = null` and `countryId = null` return the same rows. |
| Negative operators | `NotEqual` and `NotContains` with a value assume the property is not null: a `null` property does not match (like SQL `<>`), including inside `$all` / `$none`. |

### Sorting

Sorts accept the properties of a filter type only (the type tables above), through reference navigations up to 3
levels (`role.name`), self references included (`manager.manager.name`). Never through collections.

Null values (missing references included) follow PostgreSQL: last when ascending, first when descending.

Without sort request, the order of the rows is the database one. Clients should end their sorts with a unique key
(`id`): rows with equal sort values have no guaranteed order, so they can move between pages. This is the client's
choice; the API does not add it.

### Errors and diagnostics

- An unknown path, an unsupported operator or an unparsable value throws `InvalidPaginationRequestException`, returned
  as `400` by the API (code `invalid_pagination_request`).
- Two properties with the same normalized name (`UserName` / `User_Name`): only the first is usable, warning
  `PAG001` at build time.
- Two paginated entities with the same name in different namespaces: the second extensions class is suffixed
  (`User2PaginationExtensions`), info `PAG002`.

## Known limits

- **Inverting the operator does not invert the meaning** without quantifier: `posts.title != "x"` means "at least one
  post whose title is not x". Use `posts.$none.title = "x"` for "no post titled x".
- **`$all` is true on an empty collection**: combine it with `posts.$count > 0` to require at least one item.
- **`$` segments are reserved** and only valid right after a collection (`role.$none.name` is rejected). `$count`
  ends the path; on a collection of entities, `$none` / `$all` must be followed by an item property.
- **A filter takes a single value**: no `In [a, b, c]` (combine `Equal` filters with `Or`).
- **No operator precedence**: logic is evaluated left to right, so `A OR B AND C` is `(A OR B) AND C`. Use nested
  `filters` for another grouping: a nested group is evaluated first, whatever its position, so
  `[X, A (Or) { B (And) }]` is `X OR (A AND B)`.
- **Case sensitivity**: string `Equal` / `NotEqual` are case-sensitive, text operators are not. The `lower()` calls of
  text operators prevent the use of regular indexes on large tables.
- **Every property is filterable, sensitive ones included.** Every public property of a supported type (and every
  navigation) of the entity and of every reachable entity can be used in filters, and the scalar ones in sorts
  (`AuthId`, `Email`...). Filters only build parameterized queries, so no SQL injection is possible, but a client can
  infer values that the response does not return by filtering on them. This is accepted by design. Property names are
  the entity ones, not the API response ones.
- **Unlisted types are ignored**: `short`, `float`, `TimeSpan`, `TimeOnly`, custom structs... are neither filterable
  nor sortable until they are added to `ScalarFilterEmitter` (see below).

## How the generator is organized

Everything is in `Src/Services/MyApp.Services.Generators/`:

- `PaginationGenerator.cs` finds the `[PaginationHandlerFor<TEntity>]` roots, walks the navigation graph and emits:
  - one `{Entity}FilterBuilder` per reachable entity (internal, namespace `MyApp.Services.Generated.Filtering`);
  - the shared `FilterExpressionHelper`;
  - the `{Entity}PaginationExtensions` of each root.

  The file starts with a reading guide and a walkthrough of one filter down to the SQL: start there.
- `Parser/ScalarFilterEmitter.cs` emits the parsing and comparison code for each scalar type (and for `$count`).
- `AnalyzerReleases.*.md` tracks the releases of the generator diagnostics (`PAG001`, `PAG002`).

To inspect the generated code, build with:

```bash
dotnet build Src/Services/MyApp.Services --no-incremental -p:EmitCompilerGeneratedFiles=true
```

and look under `Src/Services/MyApp.Services/obj/Debug/net11.0/generated/`.

## Adding a filterable type

Every filterable type is described in `ScalarFilterEmitter.TryGetInfo` by a `ScalarInfo`:

| Argument     | Meaning |
|--------------|---------|
| `typeName`   | Fully qualified, non-nullable type name (use the `name` variable). |
| `isNullable` | Pass the `isNullable` variable: `Nullable<T>` is unwrapped before, and gets `IS NULL` support for free. |
| `operators`  | `OperatorSet.Equatable` (equal, not equal), `Comparable` (+ ordering) or `String` (+ text operators). |
| `parse`      | C# boolean expression parsing `request.Value` into `out var parsed`. Use the invariant culture. |
| `converted`  | Expression turning `parsed` into the compared value (usually `"parsed"`, `"parsed.ToUniversalTime()"` for dates). |

Once declared, the type is filterable and sortable everywhere: nullable version, through navigations, and as the
element of a collection of values (`List<TimeOnly>`, `TimeOnly[]`...). The generated code picks it up at the next
build. Then update the type tables of this page and of the README.

There are three cases, depending on the type.

**1. Type with a `SpecialType`** (most C# keyword types: `short`, `byte`, `float`, `uint`...). Add it to the
matching `case` of the `switch (type.SpecialType)`. Example for `short` and `float`, which parse like the other
numbers:

```csharp
case SpecialType.System_Int16:
case SpecialType.System_Single:
case SpecialType.System_Int32:
// ...
    info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
        $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
    return true;
```

**2. Framework type without `SpecialType`** (`TimeOnly`, `TimeSpan`...). Add a `case` on its fully qualified name in
the `switch (name)`:

```csharp
case "global::System.TimeOnly":
    info = new ScalarInfo(name, isNullable, OperatorSet.Comparable,
        $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
    return true;
```

**3. Custom type** (strongly typed id, value object `struct`...). Same as 2 with its own name and parsing, for
example a `record struct UserId(Guid Value)` implementing `IParsable<UserId>`:

```csharp
case "global::MyApp.Domain.Models.Models.UserId":
    info = new ScalarInfo(name, isNullable, OperatorSet.Equatable,
        $"{name}.TryParse(request.Value, CultureInfo.InvariantCulture, out var parsed)", "parsed");
    return true;
```

Constraints for custom types:

- the comparison is built with `Expression.Equal` / `Expression.GreaterThan`..., so the type must define the matching
  operators (`==`, `!=`, and `<`, `>`... for `Comparable`); records define `==` / `!=` automatically;
- EF Core must be able to translate the comparison: the type needs a value converter in its entity configuration,
  and ordering operators compare the **converted** database value, so only use `Comparable` when that order is
  meaningful;
- supported scalars are checked before navigations, so a type declared in `TryGetInfo` is always handled as a
  scalar. Without that declaration, a class of `MyApp.Domain.Models` is treated as a navigation (prefer `struct`s
  for value objects meant to be filtered as a single value).

Always check the translation against PostgreSQL after adding a type (for example `uint` has no native PostgreSQL
type).

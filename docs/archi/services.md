# Services

> **Read this when** you add a use case, a shared query condition (specification), a projection or a business error.

The application layer is split in two projects:

- `MyApp.Services.Abstractions` holds the **contracts**: interfaces, requests, responses. It depends on nothing.
- `MyApp.Services` holds the **implementations**, querying `AppDbContext` directly.

There is no repository layer. Shared query conditions are factored out as specifications, and shared selections as
projections.

## Adding a service

One service is one use case, named `<Entity><Action>`: `UserGetById`, `UserDeleteById`, `AuthSyncUser`.

### 1. The contract

Create `Src/Services/MyApp.Services.Abstractions/Services/<Domain>Services/I<Entity><Action>.cs` and inherit one of
the base interfaces of `IService.cs`:

| Interface                                | Returns                          |
|------------------------------------------|----------------------------------|
| `IServiceAsync<TQuery>`                  | `Task<ServiceResponse>`          |
| `IServiceAsync<TQuery, TResult>`         | `Task<ServiceResponse<TResult>>` |
| `IService<TQuery>` / `<TQuery, TResult>` | synchronous equivalents          |

When the input has several fields, declare it as a nested record named `Request`:

```csharp
public interface IAuthSyncUser : IServiceAsync<IAuthSyncUser.Request>
{
    public sealed record Request(string AuthId, string UserName, string Email);
}
```

Outputs follow the same logic:

- an output specific to one service is a nested record named `Response` in its interface:

  ```csharp
  public interface IProductGetStock : IServiceAsync<Guid, IProductGetStock.Response>
  {
      public sealed record Response(Guid ProductId, int Quantity);
  }
  ```

- an output shared by several services (typically `Get` and `GetById`) goes in
  `Services.Abstractions/Responses/<Entity>ServiceResponse.cs`.

### 2. The implementation

Create `Src/Services/MyApp.Services/Services/<Domain>Services/<Entity><Action>.cs`:

- a `public sealed class`, with constructor injection into `private readonly` fields;
- inject `AppDbContext` and `ICacheService` as needed (see [Cache](cache.md)).

### 3. Return values

Return values use the `ServiceResponse` unions (`Services.Abstractions/ServiceResponse.cs`):

- success without payload: `new ServiceSuccessResponse()`;
- success with payload: the `TResult` value itself (implicit conversion);
- error: a factory of `ServiceErrorResponse` (see [Errors](#errors) below), for example
  `ServiceErrorResponse.NotFound(ServiceConstant.Error.user_not_found)`.

### 4. Registration

Nothing to do: the service is registered automatically (see
[Host composition](host-composition.md#automatic-service-registration)).

For a paginated list, see [Pagination generator](pagination-generator.md#adding-a-paginated-service).

## Specifications

A specification is a reusable, composable predicate. Use one when the **same condition is needed by several
services** ("user has this id", "entity is active and owned by X"). It is not a repository: services keep querying
`AppDbContext` directly, specifications only factor out shared conditions.

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

`Domain.Specifications` only knows entities and expression trees: it never references EF Core APIs. The predicate
must stay translatable by EF Core, so no method calls the provider cannot translate.

## Projections

Projections live in `Src/Services/MyApp.Services/ServiceProjections/<Entity>Projection.cs`, as pairs:

```csharp
internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user => new ... { };
internal static readonly Func<User, UserServiceResponse> ToUserServiceResponseFunc = ToUserServiceResponse.Compile();
```

- Use the `Expression` in `IQueryable.Select(...)`, so EF Core only selects the needed columns.
- Use the compiled `Func` on in-memory objects.

## Errors

The API returns no translated message. An error is a stable **code** with optional **parameters**, and the client
builds the message it displays from them.

Services return a *kind* of error, never an HTTP status. `ServiceErrorResponse` is a union of the kinds, nested in
the `ServiceResponse` unions, and the host maps each kind to its status:

| Factory                          | Case             | Status |
|----------------------------------|------------------|--------|
| `ServiceErrorResponse.NotFound`  | `NotFoundError`  | `404`  |
| `ServiceErrorResponse.Conflict`  | `ConflictError`  | `409`  |
| `ServiceErrorResponse.Forbidden` | `ForbiddenError` | `403`  |
| `ServiceErrorResponse.Invalid`   | `InvalidError`   | `400`  |

`401` is not a service error: authentication answers it before any service runs.

### Adding an error

1. Add the code in `ServiceConstant.Error` (`public const string my_error = nameof(my_error);`, snake_case). Codes
   produced by the host itself (`validation_failed`, `invalid_pagination_request`) are in `ApiConstant.Error`.
2. Return it through the factory of its kind. Use the factory, not `new NotFoundError(...)`: C# applies a single
   user-defined conversion, so a case of the nested union does not convert to `ServiceResponse` by itself.

   ```csharp
   return ServiceErrorResponse.NotFound(ServiceConstant.Error.user_not_found,
       new Dictionary<string, object?> { [nameof(User.Id)] = id });
   ```

3. Parameter names are the C# ones (`nameof(...)`, PascalCase): the host converts them to camelCase. Once published,
   a code and its parameter names are part of the API contract: renaming the property behind a `nameof` renames the
   parameter for the clients.

### What the client receives

`EndpointHelper.FormatErrorResponse` returns an RFC 9457 problem with the code and the parameters as extensions. The
`type` is left to ASP.NET (the RFC 9110 link of the status): the client identifies the error by its `code`.

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "instance": "/users/0193...",
  "code": "user_not_found",
  "params": { "id": "0193..." },
  "traceId": "00-4bf92f..."
}
```

A new kind of error is a new case of the union and a new line in the `switch` of `FormatErrorResponse`. The code does
not compile until the kind is mapped.

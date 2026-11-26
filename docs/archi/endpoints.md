# Endpoints and API contracts

> **Read this when** you add or change a route, an API request or response, or a validator.

Endpoints are ASP.NET Core Minimal APIs, versioned with Asp.Versioning. They are a thin layer: a handler calls one
service and maps its result to HTTP. The HTTP contracts (`ApiRequest`, `ApiResponse`) are separate from the service
contracts: **services never return API types, and endpoints never expose service types.**

## Adding an endpoint

### 1. The API builder (new area only)

If the feature is a new area, add an API builder in `Endpoints/ApiBuilder.cs`. Always create the versioned API with
`NewAppVersionedApi(...)`, never `NewVersionedApi(...)` directly: it adds the conventions shared by every endpoint,
such as automatic validation.

```csharp
public sealed class ProductApiBuilder(IVersionedEndpointRouteBuilder builder) : VersionedApiBuilder(builder);

public static ProductApiBuilder MapProduct(this IEndpointRouteBuilder builder)
{
    return new ProductApiBuilder(builder.NewAppVersionedApi("Product").RequireAuthorization());
}
```

### 2. The routes

Create `Endpoints/<Area>Endpoint.cs` with a `ToV1()` extension declaring routes on a versioned group, and the
handlers in a `private static class V1`:

```csharp
var api = builder.MapGroup("/products").HasApiVersion(1.0);

api.MapGet("{id}", V1.GetById)
    .Produces<ProductApiResponse>()
    .Produces<ProblemHttpResult>(StatusCodes.Status404NotFound);
```

Declare every possible status code with `.Produces...`, so the OpenAPI document is accurate.

### 3. The handler

A handler injects the service with `[FromServices]`, calls it, then maps the union with an exhaustive `switch`:

```csharp
return result switch
{
    ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
    ProductServiceResponse response => TypedResults.Ok(new ProductApiResponse(response))
};
```

### 4. Registration

Register the area in `UseAppEndpoints()` (`ApiEndpointConfiguration.cs`): `app.MapProduct().ToV1();`.

### Special cases

- **Paginated lists** use the `QUERY` HTTP method with the pagination request in the body:
  `api.MapMethods(string.Empty, ["QUERY"], V1.Get)`. See [Pagination generator](pagination-generator.md).
- **Admin-only routes** add `.RequireAuthorization(ApiConstant.AuthorizationPolicies.Admin)`.
- **A new version** adds a `ToV2()` and a `V2` class next to the existing ones.

## API contracts

API contracts live in `Host.Api`:

- `Requests/<Name>ApiRequest.cs`: a `sealed record` with a `ToServiceRequest()` method converting it to the service
  request. Its FluentValidation validator is a nested `sealed class Validator : AbstractValidator<...>`.
- `Responses/<Name>ApiResponse.cs`: a `sealed record` with a constructor taking the service response. The only
  exception is `ValidationErrorApiResponse`, built by the host from a validation failure.

## Validation

Validation is automatic:

- `AddAppValidation()` registers every validator of the API assembly;
- `ValidationFilterHelper` (added by `NewAppVersionedApi`) validates every handler argument that has a validator,
  before the handler runs;
- invalid input returns a `400` validation problem (`EndpointHelper.FormatValidationErrorResponse`).

Never inject `IValidator<T>` nor call `Validate` in a handler.

Nested requests (`SortApiRequest`, `FilterApiRequest`) have their validator nested in them, applied with
`RuleForEach(...).SetValidator(...)`. A recursive request validates its children with `SetValidator(this)`.

### The validation problem

A validation problem has the code `validation_failed` and one item per invalid field in `errors`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "code": "validation_failed",
  "errors": [
    { "pointer": "/pageNumber", "code": "greater_than", "params": { "comparisonValue": 0 },
      "detail": "'Page Number' must be greater than '0'." },
    { "pointer": "/filterRequests/0/filters/1/propertyName", "code": "required",
      "detail": "'Property Name' must not be empty." }
  ]
}
```

| Field     | Content |
|-----------|---------|
| `pointer` | The invalid field as an RFC 6901 JSON pointer on the request body, in camelCase (`""` for a rule on the whole request). |
| `code`    | Built-in FluentValidation codes are mapped to API codes in `EndpointHelper.ValidatorCodes` (`NotEmptyValidator` → `required`, `GreaterThanValidator` → `greater_than`...); an unmapped built-in validator gets `invalid`. A code set with `.WithErrorCode(...)` is returned as is. |
| `params`  | The placeholder values of the rule (`comparisonValue`, `maxLength`...), in camelCase. The ones describing the property itself are removed (`PropertyName`, `PropertyValue`, `PropertyPath`, `CollectionIndex`, `ComparisonProperty`): the submitted value is never sent back. |
| `detail`  | The FluentValidation message, always in English (`LanguageManager` disabled in `AddAppValidation()`). It is a debugging aid: clients display a message built from `code` and `params`. |

Give business rules their own code (constant in `ApiConstant.Error`), and map a built-in validator in
`ValidatorCodes` when a rule starts using it.

Business errors returned by services are described in [Services](services.md#errors).

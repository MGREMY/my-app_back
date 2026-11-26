# Cross-cutting concerns

> **Read this when** you work on authentication, users, authorization, CORS, error responses, soft deletion,
> versioning, OpenAPI, health checks, file storage or logging.

Each section below describes how one concern behaves in the whole API, and where it is configured.

## Authentication

JWT bearer, with the authority and audience from `Api:Auth` (Keycloak in development). HTTPS metadata is only
disabled in development.

Current user data is read through `ClaimsPrincipalExtension`: `user.CurrentId`, `user.CurrentUserName` (from the
`preferred_username` claim), `user.CurrentEmail`. A token missing one of them is an invalid request (`400`).

## User synchronization

The client calls `POST /auth/sync-user` at each sign-in:

- it only inserts the user the first time, and never updates an existing one (this may change with the BFF);
- two concurrent first syncs: the loser gets `409`;
- a soft-deleted user gets `403`, on which the client signs out. `sync-user` is the only route returning it for now.

## Users

Rights are deliberately minimal, so that projects built on the template add their own:

- every authenticated user can list users and read any of them;
- admins delete users, never their own account (`400`);
- a soft-deleted user is hidden like an unknown one: reading or deleting it returns `404` (`user_not_found`).

## Authorization

Policies are declared in `AuthorizationConfiguration`; policy names and roles are in `ApiConstant`.

## Antiforgery

Header `X-XSRF-TOKEN`, cookie `XSRF-TOKEN`, token issued by `GET /misc/anti-forgery-token`. Routes do not validate it
yet: the BFF will handle it.

## CORS

A default policy is registered in `AddAppCors()`: allowed origins from `Api:Cors:Origins`, credentials allowed,
`Content-Disposition` exposed.

## Errors

Every error response is an RFC 9457 problem (`application/problem+json`).

- `AddAppProblemDetails()` adds `instance` (the request path) and `traceId` to every problem.
- `UseAppProblemDetails()` (`UseStatusCodePages`) gives a problem to the responses sent without body: `401` / `403`
  from authentication, unknown routes.
- Problems are always JSON: `JsonProblemDetailsWriter`, registered after the default writer, writes the ones it
  declines (an `Accept` header without JSON, such as `application/xml`) with the same content.
- Problems keep the default `title` of their status: the `code` identifies the error.

Where each error comes from:

| Source | Response |
|--------|----------|
| Unhandled exception | Logged, `500` |
| Unparsable route value (a non-GUID `id`) | `400` problem |
| Invalid input | `400` validation problem, code `validation_failed` (see [Endpoints](endpoints.md#validation)) |
| `InvalidPaginationRequestException` | `400` problem, code `invalid_pagination_request` (see [Pagination generator](pagination-generator.md)) |
| Business error (`ServiceErrorResponse`) | `EndpointHelper.FormatErrorResponse` (see [Services](services.md#errors)) |

## Culture

The API is not localized. Values are parsed and formatted with the invariant culture, and messages (validation
messages included) are not meant to be displayed: clients rely on error codes.

## Soft deletion

Entities implementing `ISoftDeletable` are filtered by the named query filter `ModelConstant.SoftDeletionFilter`. To
read deleted rows, use `.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])` and explain why in a comment.

## Versioning

The version is read from the query string or the `X-Api-Version` header. Default: `1.0`.

## OpenAPI and Scalar

Mapped in development only, with one document per API version. The UI is at `/scalar`.

## Health checks

Mapped in every environment by `MapDefaultEndpoint`:

- `/health` runs every check (disk storage, PostgreSQL, Redis) and returns a detailed JSON report;
- `/alive` only runs the checks tagged `ServiceDefaultConstant.LiveTag` (the `self` check), for liveness probes.

## File storage

`IFileStorageService` (`MyApp.Services.IO`) works with file names relative to `Api:Services:DataPath`: every method
takes and returns such names. Names resolving outside of that folder (absolute paths, `..`) are rejected with an
`ArgumentException`.

## Logging and telemetry

Serilog (console, file, OpenTelemetry sink). OTLP export is enabled when `OTEL_EXPORTER_OTLP_ENDPOINT` is set.

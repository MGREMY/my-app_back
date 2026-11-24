namespace MyApp.Host.Api.Tests.TestInfrastructure;

// JSON shape of the API responses, as a client reads them. The API records have two constructors (one taking the
// service response), which System.Text.Json cannot choose from: these mirror their wire format instead.

public sealed record UserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email);

public sealed record PageResponse<T>(int PageNumber, int PageSize, bool HasNextPage, bool HasPreviousPage, int TotalPages, T[] Data);

namespace MyApp.Services.Abstractions.Responses;

public class PaginationServiceResponse
{
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required bool HasNextPage { get; init; }
    public required bool HasPreviousPage { get; init; }
    public required int TotalPages { get; init; }
}

public sealed class PaginationServiceResponse<T> : PaginationServiceResponse
{
    public required IEnumerable<T> Data { get; init; }
}
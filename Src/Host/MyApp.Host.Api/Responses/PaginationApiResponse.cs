using MyApp.Services.Abstractions.Responses;

namespace MyApp.Host.Api.Responses;

public sealed record PaginationApiResponse<T>(
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage,
    int TotalPages,
    IEnumerable<T> Data
)
{
    public PaginationApiResponse(PaginationServiceResponse value)
        : this(
            value.PageNumber,
            value.PageSize,
            value.HasNextPage,
            value.HasPreviousPage,
            value.TotalPages,
            [])
    {
    }
}
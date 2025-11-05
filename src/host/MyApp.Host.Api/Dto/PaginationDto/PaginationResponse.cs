using MyApp.Domain.Service.Contract.Dto.PaginationDto;

namespace MyApp.Host.Api.Dto.PaginationDto;

public record PaginationResponse<T>(
    int PageNumber,
    int PageSize,
    bool HasNextPage,
    bool HasPreviousPage,
    int TotalPages,
    IEnumerable<T> Data
)
{
    public PaginationResponse(PaginationServiceResponse value)
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
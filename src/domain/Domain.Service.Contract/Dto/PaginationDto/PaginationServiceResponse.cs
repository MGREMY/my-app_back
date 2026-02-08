namespace Domain.Service.Contract.Dto.PaginationDto;

public class PaginationServiceResponse
{
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
    public required bool HasNextPage { get; set; }
    public required bool HasPreviousPage { get; set; }
    public required int TotalPages { get; set; }
}

public sealed class PaginationServiceResponse<T> : PaginationServiceResponse
{
    public required IEnumerable<T> Data { get; set; }
}
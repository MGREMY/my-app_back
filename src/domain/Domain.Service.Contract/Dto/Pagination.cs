namespace Domain.Service.Contract.Dto;

public sealed record FilterRequest(
    string PropertyName,
    FilterRequest.Operator FilterOperator,
    string Value,
    FilterRequest.Logic FilterLogic,
    IEnumerable<FilterRequest> Filters)
{
    public enum Operator
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5,
        Contains = 6,
        NotContains = 7,
        StartWith = 8,
        EndWith = 9,
    }

    public enum Logic
    {
        And = 0,
        Or = 1,
    }
}

public sealed record SortRequest(string PropertyName, bool IsDescending);

public sealed record PaginationRequest(
    int PageNumber,
    int PageSize,
    IEnumerable<SortRequest> SortRequests,
    IEnumerable<FilterRequest> FilterRequests);

public class PaginationResponse
{
    public required int PageNumber { get; set; }
    public required int PageSize { get; set; }
    public required bool HasNextPage { get; set; }
    public required bool HasPreviousPage { get; set; }
    public required int TotalPages { get; set; }
}

public sealed class PaginationResponse<T> : PaginationResponse
{
    public required IEnumerable<T> Data { get; set; }
}
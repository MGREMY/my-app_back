namespace Domain.Service.Contract.Dto.PaginationDto;

public sealed record FilterServiceRequest(
    string PropertyName,
    FilterServiceRequest.Operator FilterOperator,
    string Value,
    FilterServiceRequest.Logic FilterLogic,
    IEnumerable<FilterServiceRequest> Filters)
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

public sealed record SortServiceRequest(string PropertyName, bool IsDescending);

public sealed record PaginationServiceRequest(
    int PageNumber,
    int PageSize,
    IEnumerable<SortServiceRequest> SortServiceRequest,
    IEnumerable<FilterServiceRequest> FilterServiceRequests);
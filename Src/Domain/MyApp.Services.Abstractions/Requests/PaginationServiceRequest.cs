namespace MyApp.Services.Abstractions.Requests;

public sealed record PaginationServiceRequest(
    int PageNumber,
    int PageSize,
    IEnumerable<SortServiceRequest> SortRequests,
    IEnumerable<FilterServiceRequest> FilterRequests);
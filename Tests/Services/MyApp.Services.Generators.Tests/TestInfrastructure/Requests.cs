using MyApp.Services.Abstractions.Requests;

namespace MyApp.Services.Generators.Tests.TestInfrastructure;

/// <summary>Short factories for pagination requests, so tests read like the request they send.</summary>
internal static class Requests
{
    public static FilterServiceRequest Filter(
        string path,
        FilterServiceRequest.Operator op,
        string? value,
        FilterServiceRequest.Logic logic = FilterServiceRequest.Logic.And,
        params FilterServiceRequest[] nested)
    {
        return new FilterServiceRequest(path, op, value, logic, nested);
    }

    public static SortServiceRequest Asc(string path) => new(path, false);

    public static SortServiceRequest Desc(string path) => new(path, true);

    public static PaginationServiceRequest Page(
        int pageNumber = 1,
        int pageSize = 100,
        SortServiceRequest[]? sorts = null,
        FilterServiceRequest[]? filters = null)
    {
        return new PaginationServiceRequest(pageNumber, pageSize, sorts ?? [], filters ?? []);
    }
}

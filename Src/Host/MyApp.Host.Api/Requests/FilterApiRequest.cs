using MyApp.Services.Abstractions.Requests;

namespace MyApp.Host.Api.Requests;

public sealed record FilterApiRequest(
    string PropertyName,
    FilterApiRequest.Operator FilterOperator,
    string Value,
    FilterApiRequest.Logic FilterLogic,
    IEnumerable<FilterApiRequest>? Filters = null)
{
    public enum Logic
    {
        And = 0,
        Or = 1
    }

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
        EndWith = 9
    }

    public FilterServiceRequest ToServiceRequest()
    {
        return new FilterServiceRequest(
            PropertyName,
            (FilterServiceRequest.Operator)FilterOperator,
            Value,
            (FilterServiceRequest.Logic)FilterLogic,
            Filters?.Select(x => x.ToServiceRequest()) ?? []);
    }
}
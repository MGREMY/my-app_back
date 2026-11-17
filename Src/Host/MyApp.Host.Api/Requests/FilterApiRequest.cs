namespace MyApp.Host.Api.Requests;

public sealed record FilterApiRequest(
    string PropertyName,
    FilterApiRequest.Operator FilterOperator,
    string Value,
    FilterApiRequest.Logic FilterLogic,
    IEnumerable<FilterApiRequest>? Filters = null)
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

    public Services.Abstractions.Requests.FilterServiceRequest ToServiceRequest()
    {
        return new Services.Abstractions.Requests.FilterServiceRequest(
            PropertyName,
            (Services.Abstractions.Requests.FilterServiceRequest.Operator)FilterOperator,
            Value,
            (Services.Abstractions.Requests.FilterServiceRequest.Logic)FilterLogic,
            Filters?.Select(x => x.ToServiceRequest()) ?? []);
    }
}
namespace MyApp.Host.Api.Requests;

public sealed record SortApiRequest(string PropertyName, bool IsDescending)
{
    public Services.Abstractions.Requests.SortServiceRequest ToServiceRequest()
    {
        return new Services.Abstractions.Requests.SortServiceRequest(PropertyName, IsDescending);
    }
}
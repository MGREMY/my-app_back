using MyApp.Services.Abstractions.Requests;

namespace MyApp.Host.Api.Requests;

public sealed record SortApiRequest(string PropertyName, bool IsDescending)
{
    public SortServiceRequest ToServiceRequest()
    {
        return new SortServiceRequest(PropertyName, IsDescending);
    }
}
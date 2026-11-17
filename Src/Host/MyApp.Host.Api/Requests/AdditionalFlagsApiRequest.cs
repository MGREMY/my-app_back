namespace MyApp.Host.Api.Requests;

public record AdditionalFlagsApiRequest(bool IncludeDeletedItems)
{
    public Services.Abstractions.Requests.AdditionalFlagsServiceRequest ToServiceRequest()
    {
        return new(
            IncludeDeletedItems: IncludeDeletedItems
        );
    }
}
using System.Reflection;

namespace Host.Api.Dto;

public record AdditionalFlagsRequest(bool IncludeDeletedItems)
{
    public ServiceDto.AdditionalFlags ToServiceRequest()
    {
        return new(
            IncludeDeletedItems: IncludeDeletedItems
        );
    }

    public static ValueTask<AdditionalFlagsRequest?> BindAsync(HttpContext context, ParameterInfo _)
    {
        var includeDeletedItems =
            bool.TryParse(context.Request.Query["includeDeletedItems"].FirstOrDefault(), out var idi) && idi;

        return ValueTask.FromResult<AdditionalFlagsRequest?>(
            new AdditionalFlagsRequest(
                IncludeDeletedItems: includeDeletedItems
            ));
    }
}
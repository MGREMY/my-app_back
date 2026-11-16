using Microsoft.AspNetCore.Authorization;
using MyApp.Host.Api.Dto;

namespace MyApp.Host.Api.EndpointFilter;

public sealed class AdditionalFlagsEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var additionalFlagsRequest = context.Arguments.OfType<AdditionalFlagsRequest>().FirstOrDefault();

        if (additionalFlagsRequest is null) return await next(context);

        return additionalFlagsRequest switch
        {
            { IncludeDeletedItems: true } => await CanUserExecAction(context)
                ? await next(context)
                : TypedResults.Forbid(),
            _ => await next(context),
        };
    }

    private static async Task<bool> CanUserExecAction(EndpointFilterInvocationContext context)
    {
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthorizationService>();

        var authResult = await authService.AuthorizeAsync(
            user: context.HttpContext.User,
            resource: null,
            policyName: ApiConstant.AuthorizationPolicies.Admin);

        return authResult.Succeeded;
    }
}
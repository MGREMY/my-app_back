using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint;

public static class Misc
{
    public static RouteGroupBuilder UseMiscApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/misc").WithTags("misc");

        group.MapGet("anti-forgery-token", HandleGetAntiForgeryTokenV1).MapToApiVersion(1).AllowAnonymous();

        return g;
    }

    private static NoContent HandleGetAntiForgeryTokenV1(IAntiforgery antiForgery, HttpContext context)
    {
        antiForgery.GetAndStoreTokens(context);

        return TypedResults.NoContent();
    }
}
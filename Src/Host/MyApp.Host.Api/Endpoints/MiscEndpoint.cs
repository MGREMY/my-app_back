using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Host.Api.Endpoints;

public static class MiscEndpoint
{
    public static MiscApiBuilder ToV1(this MiscApiBuilder apiBuilder)
    {
        var builder = apiBuilder.Endpoints;
        var api = builder.MapGroup("/misc")
            .HasApiVersion(1.0);

        api.MapGet("anti-forgery-token", V1.AntiForgeryToken)
            .Produces(StatusCodes.Status204NoContent);

        return new(builder);
    }

    private static class V1
    {
        public static IResult AntiForgeryToken(
            [FromServices] IAntiforgery antiForgery,
            HttpContext context)
        {
            antiForgery.GetAndStoreTokens(context);

            return TypedResults.NoContent();
        }
    }
}
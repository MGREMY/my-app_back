using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.Extension;
using MyApp.Host.Api.Helpers;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.AuthServices;

namespace MyApp.Host.Api.Endpoints;

public static class AuthEndpoint
{
    public static AuthApiBuilder ToV1(this AuthApiBuilder apiBuilder)
    {
        var builder = apiBuilder.Endpoints;
        var api = builder.MapGroup("/auth")
            .HasApiVersion(1.0);

        api.MapPost("sync-user", V1.SyncUser)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemHttpResult>(StatusCodes.Status400BadRequest);

        return new AuthApiBuilder(builder);
    }

    private static class V1
    {
        public static async Task<IResult> SyncUser(
            [FromServices] IAuthSyncUser service,
            ClaimsPrincipal user,
            CancellationToken ct = default)
        {
            var result =
                await service.ExecuteAsync(
                    new IAuthSyncUser.Request(user.CurrentId, user.CurrentUserName, user.CurrentEmail), ct);

            return result switch
            {
                ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
                ServiceSuccessResponse => TypedResults.NoContent()
            };
        }
    }
}
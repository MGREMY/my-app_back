using System.Security.Claims;
using MyApp.Host.Api.Extension;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.Helpers;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.AuthServices;

namespace MyApp.Host.Api.Endpoint;

public static class Auth
{
    public static RouteGroupBuilder UseAuthApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/auth").WithTags("auth");

        group.MapPost("sync-user", AuthHandler.HandlePostSyncUserV1).MapToApiVersion(1);

        return g;
    }
}

public static class AuthHandler
{
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemHttpResult>(StatusCodes.Status400BadRequest)]
    public static async Task<IResult> HandlePostSyncUserV1(
        [FromServices] IAuthSyncUser service,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(new(user.CurrentId, user.CurrentUserName, user.CurrentEmail), ct);

        return result switch
        {
            ServiceErrorResponse error => EndpointHelpers.FormatErrorResponse(error),
            ServiceSuccessResponse => TypedResults.NoContent(),
            _ => throw new ArgumentOutOfRangeException() // Wierd as the compiler should not require this
        };
    }
}
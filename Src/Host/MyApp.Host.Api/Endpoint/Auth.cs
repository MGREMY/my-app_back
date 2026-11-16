using System.Security.Claims;
using MyApp.Host.Api.Extension;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.Helpers;
using MyApp.Service.Abstractions.Service.Auth;

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
    public static async Task<IResult> HandlePostSyncUserV1(
        [FromServices] ISyncUserService service,
        ClaimsPrincipal user,
        CancellationToken ct = default)
        => await EndpointHelpers.HandleAsync(service,
            new(user.CurrentId, user.CurrentUserName, user.CurrentEmail),
            ct);
}
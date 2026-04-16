using System.Security.Claims;
using Domain.Service.Contract.Service.Auth;
using Host.Api.Extension;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint;

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
    public static async Task<NoContent> HandlePostSyncUserV1(ISyncUserService service,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        await service.ExecuteAsync(new(user.CurrentId, user.CurrentUserName, user.CurrentEmail), ct);

        return TypedResults.NoContent();
    }
}
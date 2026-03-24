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

        group.MapPost("sync-user", HandleGetSyncUserV1).MapToApiVersion(1);

        return g;
    }

    private static async Task<NoContent> HandleGetSyncUserV1(IAuthSyncUserService service,
        ClaimsPrincipal user,
        CancellationToken ct = default)
    {
        await service.ExecuteAsync(new(user.CurrentId, user.CurrentUserName, user.CurrentEmail), ct);

        return TypedResults.NoContent();
    }
}
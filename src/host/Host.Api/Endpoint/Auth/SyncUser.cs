using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using Domain.Service.Contract.Service.AuthService;
using FastEndpoints;
using Host.Api.Extension;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint.Auth;

public sealed class SyncUser_V1 : Ep.NoReq.Res<NoContent>
{
    private readonly IAuthSyncUserService _service;

    public SyncUser_V1(IAuthSyncUserService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Post("/auth/sync-user");
        Version(1);
    }

    public override async Task<NoContent> ExecuteAsync(CancellationToken ct)
    {
        await _service.ExecuteAsync(
            new AuthSyncUserServiceRequest(User.CurrentId, User.CurrentUserName, User.CurrentEmail),
            ct);

        return TypedResults.NoContent();
    }
}
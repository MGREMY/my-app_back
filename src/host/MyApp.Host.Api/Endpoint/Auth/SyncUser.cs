using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using MyApp.Domain.Service.Contract.Service.AuthService;

namespace MyApp.Host.Api.Endpoint.Auth;

public class SyncUser_V1 : Ep.NoReq.Res<NoContent>
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
            new AuthSyncUserServiceRequest(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value),
            ct);

        return TypedResults.NoContent();
    }
}
using Domain.Service.Contract.Service.UserService;
using FastEndpoints;
using Host.Api.Dto.UserDto.UserDeleteDto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint.User;

public class Delete_V1 : Ep.Req<UserDeleteRequest>.Res<Results<NoContent, NotFound>>
{
    private readonly IUserDeleteService _service;

    public Delete_V1(IUserDeleteService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Delete("/users/{id}");
        Version(1);
        Policies(ApiConstant.AuthorizationPolicies.Admin);
    }

    public override async Task<Results<NoContent, NotFound>> ExecuteAsync(
        UserDeleteRequest req,
        CancellationToken ct)
    {
        await _service.ExecuteAsync(req.ToServiceRequest(), ct);

        return TypedResults.NoContent();
    }
}
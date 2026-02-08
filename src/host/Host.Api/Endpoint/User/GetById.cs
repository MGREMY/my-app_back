using Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;
using Domain.Service.Contract.Service.UserService;
using FastEndpoints;
using Host.Api.Dto.UserDto;
using Host.Api.Dto.UserDto.UserGetByIdDto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint.User;

public sealed class GetById_V1 : Ep.Req<UserGetByIdRequest>.Res<Results<Ok<UserResponse>, NotFound>>
{
    private readonly IUserGetByIdService _service;

    public GetById_V1(IUserGetByIdService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("/users/{id}");
        Version(1);
    }

    public override async Task<Results<Ok<UserResponse>, NotFound>> ExecuteAsync(
        UserGetByIdRequest req,
        CancellationToken ct)
    {
        var result = await _service.ExecuteAsync(req.ToServiceRequest(), ct);

        return TypedResults.Ok(new UserResponse(result));
    }
}
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;
using MyApp.Domain.Service.Contract.Service.UserService;
using MyApp.Host.Api.Dto.UserDto;
using MyApp.Host.Api.Dto.UserDto.UserGetByIdDto;

namespace MyApp.Host.Api.Endpoint.User;

public class GetById_V1 : Ep.Req<UserGetByIdRequest>.Res<Results<Ok<UserResponse>, NotFound>>
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
        var result = await _service.ExecuteAsync(new UserGetByIdServiceRequest(req.Id), ct);

        return TypedResults.Ok(new UserResponse(result));
    }
}
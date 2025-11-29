using Domain.Service.Contract.Service.UserService;
using FastEndpoints;
using Host.Api.Dto.PaginationDto;
using Host.Api.Dto.UserDto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint.User;

public class Get_V1 : Ep.Req<PaginationRequest>.Res<Ok<PaginationResponse<MinimalUserResponse>>>
{
    private readonly IUserGetService _service;

    public Get_V1(IUserGetService service)
    {
        _service = service;
    }

    public override void Configure()
    {
        Get("/users");
        Version(1);
    }

    public override async Task<Ok<PaginationResponse<MinimalUserResponse>>> ExecuteAsync(
        PaginationRequest req,
        CancellationToken ct)
    {
        var result = await _service.ExecuteAsync(req.ToServiceRequest(), ct);

        return TypedResults.Ok(new PaginationResponse<MinimalUserResponse>(result)
        {
            Data = result.Data.Select(x => new MinimalUserResponse(x)),
        });
    }
}
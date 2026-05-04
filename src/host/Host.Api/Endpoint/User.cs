using Domain.Service.Contract.Service.User;
using FluentValidation;
using Host.Api.Dto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint;

public static class User
{
    public static RouteGroupBuilder UseUserApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/users").WithTags("users");

        group.MapGet(string.Empty, UserHandler.HandleGetV1).MapToApiVersion(1);
        group.MapGet("{id:guid}", UserHandler.HandleGetByIdV1).MapToApiVersion(1);

        return g;
    }
}

public static class UserHandler
{
    public static async Task<Results<Ok<PaginationResponse<MinimalUserResponse>>, BadRequest<ErrorResponse>>>
        HandleGetV1(
            PaginationRequest req,
            IValidator<PaginationRequest> validator,
            IGetUserService service,
            CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(req, ct);

        var result = await service.ExecuteAsync(req.ToServiceRequest(), ct);

        return TypedResults.Ok(new PaginationResponse<MinimalUserResponse>(result)
        {
            Data = result.Data.Select(x => new MinimalUserResponse(x)),
        });
    }

    public static async Task<Results<Ok<UserResponse>, NotFound>> HandleGetByIdV1(
        Guid id,
        IGetUserByIdService service,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(id, ct);

        return TypedResults.Ok(new UserResponse(result));
    }
}
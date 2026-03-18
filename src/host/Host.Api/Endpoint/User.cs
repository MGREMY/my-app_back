using Domain.Service.Contract.Service.UserService;
using FluentValidation;
using Host.Api.Dto;
using Host.Api.Dto.PaginationDto;
using Host.Api.Dto.UserDto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint;

public static class User
{
    public static RouteGroupBuilder UseUserApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/users").WithTags("users");

        group.MapGet(string.Empty, HandleGetUsersV1).MapToApiVersion(1);
        group.MapGet("{id:guid}", HandleGetUserByIdV1).MapToApiVersion(1);
        group.MapDelete("{id:guid}", HandleDeleteUserByIdV1).MapToApiVersion(1)
            .RequireAuthorization(ApiConstant.AuthorizationPolicies.Admin);

        return g;
    }

    private static async Task<Results<Ok<PaginationResponse<MinimalUserResponse>>, BadRequest<ErrorResponse>>>
        HandleGetUsersV1(
            PaginationRequest req,
            IValidator<PaginationRequest> validator,
            IUserGetService service,
            CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(req, ct);

        var result = await service.ExecuteAsync(req.ToServiceRequest(), ct);

        return TypedResults.Ok(new PaginationResponse<MinimalUserResponse>(result)
        {
            Data = result.Data.Select(x => new MinimalUserResponse(x)),
        });
    }

    private static async Task<Results<Ok<UserResponse>, NotFound>> HandleGetUserByIdV1(
        Guid id,
        IUserGetByIdService service,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(new(id), ct);

        return TypedResults.Ok(new UserResponse(result));
    }

    private static async Task<Results<NoContent, NotFound>> HandleDeleteUserByIdV1(
        Guid id,
        IUserDeleteService service,
        CancellationToken ct = default)
    {
        await service.ExecuteAsync(new(id), ct);

        return TypedResults.NoContent();
    }
}
using Domain.Service.Contract.Service.User;
using FluentValidation;
using Host.Api.Dto;
using Host.Api.EndpointFilter;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Host.Api.Endpoint;

public static class User
{
    public static RouteGroupBuilder UseUserApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/users").WithTags("users");

        group.MapGet(string.Empty, UserHandler.HandleGetV1)
            .AddEndpointFilter<AdditionalFlagsEndpointFilter>()
            .MapToApiVersion(1);
        group.MapGet("{id:guid}", UserHandler.HandleGetByIdV1).MapToApiVersion(1);
        group.MapDelete("{id:guid}", UserHandler.HandleDeleteByIdV1)
            .RequireAuthorization(ApiConstant.AuthorizationPolicies.Admin)
            .MapToApiVersion(1);

        return g;
    }
}

public static class UserHandler
{
    public static async Task<Results<Ok<PaginationResponse<MinimalUserResponse>>, BadRequest<ErrorResponse>>>
        HandleGetV1(
            PaginationRequest req,
            AdditionalFlagsRequest additionalFlags,
            [FromServices] IValidator<PaginationRequest> validator,
            [FromServices] IGetUserService service,
            CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(req, ct);

        var result = await service.ExecuteAsync(new(req.ToServiceRequest(), additionalFlags.ToServiceRequest()), ct);

        return TypedResults.Ok(new PaginationResponse<MinimalUserResponse>(result)
        {
            Data = result.Data.Select(x => new MinimalUserResponse(x)),
        });
    }

    public static async Task<Results<Ok<UserResponse>, NotFound>> HandleGetByIdV1(
        [FromRoute] Guid id,
        [FromServices] IGetUserByIdService service,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(id, ct);

        return TypedResults.Ok(new UserResponse(result));
    }

    public static async Task<Results<NoContent, NotFound>> HandleDeleteByIdV1(
        Guid id,
        [FromServices] IDeleteUserByIdService service,
        CancellationToken ct = default)
    {
        await service.ExecuteAsync(id, ct);

        return TypedResults.NoContent();
    }
}
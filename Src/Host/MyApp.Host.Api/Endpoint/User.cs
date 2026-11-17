using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.EndpointFilter;
using MyApp.Host.Api.Helpers;
using MyApp.Host.Api.Requests;
using MyApp.Host.Api.Responses;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;

namespace MyApp.Host.Api.Endpoint;

public static class User
{
    public static RouteGroupBuilder UseUserApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/users").WithTags("users");

        group.MapMethods(string.Empty, ["QUERY"], UserHandler.HandleGetV1)
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
    [ProducesResponseType<PaginationApiResponse<UserApiResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemHttpResult>(StatusCodes.Status400BadRequest)]
    public static async Task<IResult>
        HandleGetV1(
            [FromBody] PaginationApiRequest req,
            [FromServices] IUserGet service,
            CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(new(req.ToServiceRequest()), ct);

        return result switch
        {
            ServiceErrorResponse error => EndpointHelpers.FormatErrorResponse(error),
            PaginationServiceResponse<UserServiceResponse> response => TypedResults.Ok(
                new PaginationApiResponse<UserApiResponse>(response)
                {
                    Data = response.Data.Select(x => new UserApiResponse(x)),
                }),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [ProducesResponseType<UserApiResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemHttpResult>(StatusCodes.Status400BadRequest)]
    public static async Task<IResult> HandleGetByIdV1(
        [FromRoute] Guid id,
        [FromServices] IUserGetById service,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(id, ct);

        return result switch
        {
            ServiceErrorResponse error => EndpointHelpers.FormatErrorResponse(error),
            UserServiceResponse response => TypedResults.Ok(new UserApiResponse(response)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemHttpResult>(StatusCodes.Status404NotFound)]
    public static async Task<Results<NoContent, IResult>> HandleDeleteByIdV1(
        Guid id,
        [FromServices] IUserDeleteById service,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(id, ct);

        return result switch
        {
            ServiceErrorResponse error => EndpointHelpers.FormatErrorResponse(error),
            ServiceSuccessResponse => TypedResults.NoContent(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
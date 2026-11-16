using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.Dto;
using MyApp.Host.Api.EndpointFilter;
using MyApp.Host.Api.Helpers;
using MyApp.Service.Abstractions.Service.User;

namespace MyApp.Host.Api.Endpoint;

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
    public static async Task<IResult>
        HandleGetV1(
            PaginationRequest req,
            AdditionalFlagsRequest additionalFlags,
            [FromServices] IValidator<PaginationRequest> validator,
            [FromServices] IGetUserService service,
            CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(req, ct);

        return await EndpointHelpers.HandleAsync(service,
            new(req.ToServiceRequest(), additionalFlags.ToServiceRequest()),
            ct);
    }

    public static async Task<IResult> HandleGetByIdV1(
        [FromRoute] Guid id,
        [FromServices] IGetUserByIdService service,
        CancellationToken ct = default) => await EndpointHelpers.HandleAsync(service, id, ct);

    public static async Task<IResult> HandleDeleteByIdV1(
        Guid id,
        [FromServices] IDeleteUserByIdService service,
        CancellationToken ct = default) => await EndpointHelpers.HandleAsync(service, id, ct);
}
using Domain.Service.Contract.Service.Admin.User;
using FluentValidation;
using Host.Api.Dto;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Host.Api.Endpoint;

public static class Admin
{
    public static RouteGroupBuilder UseAdminApi(this RouteGroupBuilder g)
    {
        var group = g.MapGroup("/admin").WithTags("admin")
            .RequireAuthorization(ApiConstant.AuthorizationPolicies.Admin);

        var user = group.MapGroup("/users").WithTags("users");
        user.MapGet(string.Empty, AdminHandler.UserHandler.HandleGetUsersV1).MapToApiVersion(1);
        user.MapGet("{id:guid}", AdminHandler.UserHandler.HandleGetUserByIdV1).MapToApiVersion(1);
        user.MapDelete("{id:guid}", AdminHandler.UserHandler.HandleDeleteUserByIdV1).MapToApiVersion(1);

        return g;
    }
}

public static class AdminHandler
{
    public static class UserHandler
    {
        public static async Task<Results<Ok<PaginationResponse<MinimalUserResponse>>, BadRequest<ErrorResponse>>>
            HandleGetUsersV1(
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

        public static async Task<Results<Ok<UserResponse>, NotFound>> HandleGetUserByIdV1(
            Guid id,
            IGetUserByIdService service,
            CancellationToken ct = default)
        {
            var result = await service.ExecuteAsync(id, ct);

            return TypedResults.Ok(new UserResponse(result));
        }

        public static async Task<Results<NoContent, NotFound>> HandleDeleteUserByIdV1(
            Guid id,
            IDeleteUserService service,
            CancellationToken ct = default)
        {
            await service.ExecuteAsync(id, ct);

            return TypedResults.NoContent();
        }
    }
}
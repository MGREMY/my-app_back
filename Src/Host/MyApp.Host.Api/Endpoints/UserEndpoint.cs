using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyApp.Host.Api.Helpers;
using MyApp.Host.Api.Requests;
using MyApp.Host.Api.Responses;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;

namespace MyApp.Host.Api.Endpoints;

public static class UserEndpoint
{
    public static UserApiBuilder ToV1(this UserApiBuilder apiBuilder)
    {
        var builder = apiBuilder.Endpoints;
        var api = builder.MapGroup("/users")
            .HasApiVersion(1.0);

        api.MapMethods(string.Empty, ["QUERY"], V1.Get)
            .Produces<PaginationApiResponse<UserApiResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemHttpResult>(StatusCodes.Status400BadRequest);

        api.MapGet("{id:guid}", V1.GetById)
            .Produces<UserApiResponse>(StatusCodes.Status200OK)
            .Produces<ProblemHttpResult>(StatusCodes.Status404NotFound);

        api.MapDelete("{id:guid}", V1.DeleteById)
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemHttpResult>(StatusCodes.Status404NotFound);

        return new(builder);
    }

    private static class V1
    {
        public static async Task<IResult>
            Get(
                [FromBody] PaginationApiRequest req,
                [FromServices] IUserGet service,
                [FromServices] IValidator<PaginationApiRequest> validator,
                CancellationToken ct = default)
        {
            await validator.ValidateAndThrowAsync(req, ct);

            var result = await service.ExecuteAsync(new(req.ToServiceRequest()), ct);

            return result switch
            {
                ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
                PaginationServiceResponse<UserServiceResponse> response => TypedResults.Ok(
                    new PaginationApiResponse<UserApiResponse>(response)
                    {
                        Data = response.Data.Select(x => new UserApiResponse(x)),
                    }),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static async Task<IResult> GetById(
            [FromRoute] Guid id,
            [FromServices] IUserGetById service,
            CancellationToken ct = default)
        {
            var result = await service.ExecuteAsync(id, ct);

            return result switch
            {
                ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
                UserServiceResponse response => TypedResults.Ok(new UserApiResponse(response)),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        public static async Task<IResult> DeleteById(
            Guid id,
            [FromServices] IUserDeleteById service,
            CancellationToken ct = default)
        {
            var result = await service.ExecuteAsync(id, ct);

            return result switch
            {
                ServiceErrorResponse error => EndpointHelper.FormatErrorResponse(error),
                ServiceSuccessResponse => TypedResults.NoContent(),
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
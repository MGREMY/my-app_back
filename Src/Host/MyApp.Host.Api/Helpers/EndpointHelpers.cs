using MyApp.Host.Api.Dto;
using MyApp.Service.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelpers
{
    public static IResult Handle<TQuery>(MyApp.Service.Abstractions.IService<TQuery> service, TQuery query)
    {
        var result = service.Execute(query);

        return result switch
        {
            ServiceErrorResponse error => TypedResults.Json(
                new ErrorResponse
                {
                    StatusCode = error.StatusCode,
                    Message = error.Message
                }, statusCode: error.StatusCode),
            ServiceSuccessResponse => TypedResults.NoContent(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static IResult Handle<TQuery, TResponse>(MyApp.Service.Abstractions.IService<TQuery, TResponse> service,
        TQuery query)
    {
        var result = service.Execute(query);

        return result switch
        {
            ServiceErrorResponse error => TypedResults.Json(
                new ErrorResponse
                {
                    StatusCode = error.StatusCode,
                    Message = error.Message
                }, statusCode: error.StatusCode),
            TResponse response => TypedResults.Ok(response),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static async Task<IResult> HandleAsync<TQuery>(MyApp.Service.Abstractions.IServiceAsync<TQuery> service,
        TQuery query,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(query, ct);

        return result switch
        {
            ServiceErrorResponse error => TypedResults.Json(
                new ErrorResponse
                {
                    StatusCode = error.StatusCode,
                    Message = error.Message
                }, statusCode: error.StatusCode),
            ServiceSuccessResponse => TypedResults.NoContent(),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    public static async Task<IResult> HandleAsync<TQuery, TResponse>(
        MyApp.Service.Abstractions.IServiceAsync<TQuery, TResponse> service,
        TQuery query,
        CancellationToken ct = default)
    {
        var result = await service.ExecuteAsync(query, ct);

        return result switch
        {
            ServiceErrorResponse error => TypedResults.Json(
                new ErrorResponse
                {
                    StatusCode = error.StatusCode,
                    Message = error.Message
                }, statusCode: error.StatusCode),
            TResponse response => TypedResults.Ok(response),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }
}
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelpers
{
    public static ProblemHttpResult FormatErrorResponse(ServiceErrorResponse response)
    {
        return TypedResults.Problem(statusCode: response.StatusCode, title: response.Message);
    }
}
using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelper
{
    public static ProblemHttpResult FormatErrorResponse(ServiceErrorResponse response)
    {
        return TypedResults.Problem(statusCode: response.StatusCode, title: response.Message);
    }

    /// <summary>
    ///     Builds the <c>400</c> validation problem of the request, its error keys named like the JSON the client sends
    ///     (the naming policy of the API's <see cref="JsonOptions" />, camelCase by default).
    /// </summary>
    public static ValidationProblem FormatValidationErrorResponse(HttpContext context,
        IEnumerable<ValidationFailure> failures)
    {
        var namingPolicy = context.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value
            .SerializerOptions.PropertyNamingPolicy;

        return FormatValidationErrorResponse(failures, namingPolicy);
    }

    /// <summary>
    ///     Builds the <c>400</c> validation problem of the request. FluentValidation names the failures after the C#
    ///     properties (<c>SortRequests[0].PropertyName</c>): every segment of the path is renamed with
    ///     <paramref name="namingPolicy" /> (<c>sortRequests[0].propertyName</c>), indexes are kept.
    /// </summary>
    public static ValidationProblem FormatValidationErrorResponse(IEnumerable<ValidationFailure> failures,
        JsonNamingPolicy? namingPolicy = null)
    {
        return TypedResults.ValidationProblem(
            title: "Validation failed",
            detail: "The request contains invalid fields.",
            errors: failures
                .GroupBy(x => ToJsonPath(x.PropertyName, namingPolicy))
                .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray()));
    }

    private static string ToJsonPath(string propertyPath, JsonNamingPolicy? namingPolicy)
    {
        if (namingPolicy is null || string.IsNullOrEmpty(propertyPath)) return propertyPath;

        return string.Join('.', propertyPath.Split('.').Select(segment =>
        {
            var index = segment.IndexOf('[');

            return index < 0
                ? namingPolicy.ConvertName(segment)
                : namingPolicy.ConvertName(segment[..index]) + segment[index..];
        }));
    }
}

using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelper
{
    public static ProblemHttpResult FormatErrorResponse(ServiceErrorResponse error)
    {
        var (status, code, @params) = error switch
        {
            NotFoundError e => (StatusCodes.Status404NotFound, e.Code, e.Params),
            ConflictError e => (StatusCodes.Status409Conflict, e.Code, e.Params),
            ForbiddenError e => (StatusCodes.Status403Forbidden, e.Code, e.Params),
            InvalidError e => (StatusCodes.Status400BadRequest, e.Code, e.Params),
        };

        var extensions = new Dictionary<string, object?> { ["code"] = code };
        if (@params is { Count: > 0 })
            extensions["params"] = @params.ToDictionary(
                x => JsonNamingPolicy.CamelCase.ConvertName(x.Key),
                x => x.Value);

        return TypedResults.Problem(
            statusCode: status,
            extensions: extensions);
    }

    public static ValidationProblem FormatValidationErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        return TypedResults.ValidationProblem(
            title: "Validation failed",
            detail: "The request contains invalid fields.",
            errors: failures
                .GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage).ToArray()));
    }
}
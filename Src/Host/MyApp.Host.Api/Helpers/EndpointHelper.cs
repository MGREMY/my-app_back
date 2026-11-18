using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelper
{
    public static ProblemHttpResult FormatErrorResponse(ServiceErrorResponse response)
    {
        return TypedResults.Problem(statusCode: response.StatusCode, title: response.Message);
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

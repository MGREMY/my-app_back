using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using MyApp.Host.Api.Responses;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Helpers;

public static class EndpointHelper
{
    // Placeholders describing the property itself, not the rule: PropertyValue would echo the submitted value
    private static readonly HashSet<string> HiddenPlaceholders =
        ["PropertyName", "PropertyValue", "PropertyPath", "CollectionIndex", "ComparisonProperty"];

    // Built-in FluentValidation codes. A code set with WithErrorCode is kept as is.
    private static readonly Dictionary<string, string> ValidatorCodes = new()
    {
        ["NotEmptyValidator"] = "required",
        ["NotNullValidator"] = "required",
        ["EnumValidator"] = "invalid_value",
        ["GreaterThanValidator"] = "greater_than",
        ["GreaterThanOrEqualValidator"] = "greater_than_or_equal",
        ["LessThanValidator"] = "less_than",
        ["LessThanOrEqualValidator"] = "less_than_or_equal",
        ["InclusiveBetweenValidator"] = "between",
        ["ExclusiveBetweenValidator"] = "between_exclusive",
        ["MinimumLengthValidator"] = "min_length",
        ["MaximumLengthValidator"] = "max_length",
        ["LengthValidator"] = "length",
        ["EmailValidator"] = "invalid_email",
        ["RegularExpressionValidator"] = "invalid_format"
    };

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
                x => ToCamelCase(x.Key),
                x => x.Value);

        return TypedResults.Problem(statusCode: status, extensions: extensions);
    }

    public static ProblemHttpResult FormatValidationErrorResponse(IEnumerable<ValidationFailure> failures)
    {
        return TypedResults.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = ApiConstant.Error.validation_failed,
                ["errors"] = failures.Select(ToValidationError).ToArray()
            });
    }

    private static ValidationErrorApiResponse ToValidationError(ValidationFailure failure)
    {
        // Null on a failure built by hand (context.AddFailure in a Custom rule)
        var @params = failure.FormattedMessagePlaceholderValues?
            .Where(x => !HiddenPlaceholders.Contains(x.Key))
            .ToDictionary(x => ToCamelCase(x.Key), object? (x) => x.Value);

        return new ValidationErrorApiResponse(
            Pointer: ToJsonPointer(failure.PropertyName),
            Code: ToCode(failure.ErrorCode),
            Params: @params is { Count: > 0 } ? @params : null,
            Detail: failure.ErrorMessage);
    }

    // An unmapped built-in validator gets a generic code; a custom code (WithErrorCode) is the API's own
    private static string ToCode(string errorCode)
    {
        if (ValidatorCodes.TryGetValue(errorCode, out var code)) return code;
        if (errorCode.EndsWith("Validator", StringComparison.Ordinal)) return "invalid";

        return errorCode;
    }

    // FilterRequests[0].Filters[1].PropertyName -> /filterRequests/0/filters/1/propertyName (RFC 6901)
    private static string ToJsonPointer(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
            return string.Empty;

        var segments = propertyName
            .Split('.')
            .SelectMany(x => x.Split('[', StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.TrimEnd(']'))
            .Select(x => char.IsAsciiDigit(x[0]) ? x : ToCamelCase(x))
            .Select(x => x.Replace("~", "~0").Replace("/", "~1"));

        return "/" + string.Join('/', segments);
    }

    private static string ToCamelCase(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);
}
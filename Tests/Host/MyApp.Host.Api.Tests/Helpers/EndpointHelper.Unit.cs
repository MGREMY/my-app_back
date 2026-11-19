using FluentValidation.Results;
using MyApp.Host.Api.Helpers;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Tests.Helpers;

public sealed partial class EndpointHelperTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Theory]
        [InlineData(StatusCodes.Status403Forbidden)]
        [InlineData(StatusCodes.Status404NotFound)]
        public void FormatErrorResponse_ServiceError_ReturnsProblemWithStatusAndMessageAsTitle(int statusCode)
        {
            var result = EndpointHelper.FormatErrorResponse(new ServiceErrorResponse(statusCode, "User not found"));

            result.StatusCode.ShouldBe(statusCode);
            result.ProblemDetails.Status.ShouldBe(statusCode);
            result.ProblemDetails.Title.ShouldBe("User not found");
            result.ContentType.ShouldBe("application/problem+json");
        }

        [Fact]
        public void FormatValidationErrorResponse_Failures_Returns400GroupedByProperty()
        {
            var result = EndpointHelper.FormatValidationErrorResponse(
            [
                new ValidationFailure("PageNumber", "too small"),
                new ValidationFailure("PageSize", "too small"),
                new ValidationFailure("PageNumber", "not a number")
            ]);

            result.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
            result.ProblemDetails.Title.ShouldBe("Validation failed");
            result.ProblemDetails.Detail.ShouldBe("The request contains invalid fields.");
            result.ProblemDetails.Errors.Keys.ShouldBe(["PageNumber", "PageSize"], ignoreOrder: true);
            result.ProblemDetails.Errors["PageNumber"].ShouldBe(["too small", "not a number"]);
            result.ProblemDetails.Errors["PageSize"].ShouldBe(["too small"]);
        }

        [Fact]
        public void FormatValidationErrorResponse_NoFailure_ReturnsEmptyErrors()
        {
            EndpointHelper.FormatValidationErrorResponse([]).ProblemDetails.Errors.ShouldBeEmpty();
        }
    }
}

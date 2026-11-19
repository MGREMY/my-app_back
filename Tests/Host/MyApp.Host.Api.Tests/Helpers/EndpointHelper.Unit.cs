using System.Text.Json;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Host.Api.Helpers;
using MyApp.Services.Abstractions;

namespace MyApp.Host.Api.Tests.Helpers;

public sealed partial class EndpointHelperTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
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
        public void FormatValidationErrorResponse_NamingPolicy_RenamesEverySegmentAndKeepsIndexes()
        {
            var result = EndpointHelper.FormatValidationErrorResponse(
            [
                new ValidationFailure("PageNumber", "too small"),
                new ValidationFailure("FilterRequests[0].Filters[1].PropertyName", "required")
            ], JsonNamingPolicy.CamelCase);

            result.ProblemDetails.Errors.Keys.ShouldBe(
                ["pageNumber", "filterRequests[0].filters[1].propertyName"], ignoreOrder: true);
        }

        [Fact]
        public void FormatValidationErrorResponse_RequestContext_UsesTheApiJsonNamingPolicy()
        {
            var services = new ServiceCollection()
                .Configure<JsonOptions>(x => x.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower)
                .BuildServiceProvider();
            var context = new DefaultHttpContext { RequestServices = services };

            var result = EndpointHelper.FormatValidationErrorResponse(context,
                [new ValidationFailure("SortRequests[0].PropertyName", "required")]);

            result.ProblemDetails.Errors.Keys.ShouldBe(["sort_requests[0].property_name"]);
        }

        [Fact]
        public void FormatValidationErrorResponse_NoFailure_ReturnsEmptyErrors()
        {
            EndpointHelper.FormatValidationErrorResponse([]).ProblemDetails.Errors.ShouldBeEmpty();
        }
    }
}

using FluentValidation.Results;
using MyApp.Host.Api.Helpers;
using MyApp.Host.Api.Responses;

namespace MyApp.Host.Api.Tests.Helpers;

/// <summary>
///     Validation failures that no validator of the API produces yet, so HTTP cannot reach them: unmapped and custom
///     codes, failures added by hand, rules on the whole request.
/// </summary>
public sealed partial class EndpointHelperTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Fact]
        public void FormatValidationErrorResponse_UnmappedBuiltInValidator_ReturnsGenericCode()
        {
            var error = Format(new ValidationFailure("Name", "message") { ErrorCode = "PredicateValidator" });

            error.Code.ShouldBe("invalid");
        }

        [Fact]
        public void FormatValidationErrorResponse_CustomCode_KeepsTheCode()
        {
            var error = Format(new ValidationFailure("Name", "message") { ErrorCode = "name_already_taken" });

            error.Code.ShouldBe("name_already_taken");
        }

        [Fact]
        public void FormatValidationErrorResponse_FailureAddedByHand_ReturnsNoParams()
        {
            // context.AddFailure(new ValidationFailure(...)) in a Custom rule: no placeholder values
            var error = Format(new ValidationFailure("Name", "message") { ErrorCode = "name_already_taken" });

            error.Params.ShouldBeNull();
        }

        [Fact]
        public void FormatValidationErrorResponse_RuleOnWholeRequest_PointsToTheDocument()
        {
            var error = Format(new ValidationFailure(string.Empty, "message") { ErrorCode = "NotNullValidator" });

            error.Pointer.ShouldBe(string.Empty);
        }

        private static ValidationErrorApiResponse Format(ValidationFailure failure)
        {
            var problem = EndpointHelper.FormatValidationErrorResponse([failure]).ProblemDetails;

            return problem.Extensions["errors"].ShouldBeOfType<ValidationErrorApiResponse[]>().ShouldHaveSingleItem();
        }
    }
}

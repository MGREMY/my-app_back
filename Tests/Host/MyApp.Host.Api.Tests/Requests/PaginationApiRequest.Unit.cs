using FluentValidation.TestHelper;
using MyApp.Host.Api.Requests;

namespace MyApp.Host.Api.Tests.Requests;

/// <summary>Also covers the rules of <see cref="SortApiRequest"/> and <see cref="FilterApiRequest"/> (child rules).</summary>
public sealed partial class PaginationApiRequestTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private readonly PaginationApiRequest.Validator _validator = new();

        [Fact]
        public void Validator_DefaultRequest_IsValid()
        {
            _validator.TestValidate(new PaginationApiRequest()).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Validator_EmptySortAndFilterLists_IsValid()
        {
            _validator.TestValidate(new PaginationApiRequest(SortRequests: [], FilterRequests: [])).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validator_PageNumberNotPositive_HasError(int pageNumber)
        {
            _validator.TestValidate(new PaginationApiRequest(PageNumber: pageNumber))
                .ShouldHaveValidationErrorFor(x => x.PageNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Validator_PageSizeNotPositive_HasError(int pageSize)
        {
            _validator.TestValidate(new PaginationApiRequest(PageSize: pageSize))
                .ShouldHaveValidationErrorFor(x => x.PageSize);
        }

        [Fact]
        public void Validator_ValidSortsAndFilters_IsValid()
        {
            var request = new PaginationApiRequest(
                SortRequests: [new SortApiRequest("userName", true), new SortApiRequest("id", false)],
                FilterRequests: [Filter("userName"), Filter("email", value: null)]);

            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validator_SortWithoutPropertyName_HasIndexedError(string propertyName)
        {
            var request = new PaginationApiRequest(SortRequests: [new SortApiRequest("id", false), new SortApiRequest(propertyName, false)]);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("SortRequests[1].PropertyName");
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Validator_FilterWithoutPropertyName_HasIndexedError(string propertyName)
        {
            var request = new PaginationApiRequest(FilterRequests: [Filter(propertyName)]);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("FilterRequests[0].PropertyName");
        }

        [Fact]
        public void Validator_FilterOperatorOutOfEnum_HasError()
        {
            var request = new PaginationApiRequest(FilterRequests: [Filter("userName", @operator: (FilterApiRequest.Operator)99)]);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("FilterRequests[0].FilterOperator");
        }

        [Fact]
        public void Validator_FilterLogicOutOfEnum_HasError()
        {
            var request = new PaginationApiRequest(FilterRequests: [Filter("userName", logic: (FilterApiRequest.Logic)2)]);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("FilterRequests[0].FilterLogic");
        }

        [Fact]
        public void Validator_NullFilterValue_IsValid()
        {
            // A JSON null means IS NULL / IS NOT NULL: whether it is allowed is decided by the generated code.
            _validator.TestValidate(new PaginationApiRequest(FilterRequests: [Filter("email", value: null)]))
                .ShouldNotHaveAnyValidationErrors();
        }

        [Fact(Skip = "Bug: the validator only checks top-level filters; an invalid nested filter (Filters) is not "
                     + "validated, so the API does not return a 400 validation problem for it.")]
        public void Validator_InvalidNestedFilter_HasIndexedError()
        {
            var request = new PaginationApiRequest(FilterRequests: [Filter("userName", nested: [Filter("email"), Filter("")])]);

            _validator.TestValidate(request).ShouldHaveValidationErrorFor("FilterRequests[0].Filters[1].PropertyName");
        }

        [Fact]
        public void Validator_ValidNestedFilters_IsValid()
        {
            var request = new PaginationApiRequest(FilterRequests: [Filter("userName", nested: [Filter("email", logic: FilterApiRequest.Logic.Or)])]);

            _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ToServiceRequest_FullRequest_MapsEveryField()
        {
            var request = new PaginationApiRequest(3, 25, [new SortApiRequest("userName", true)], [Filter("email")]).ToServiceRequest();

            request.PageNumber.ShouldBe(3);
            request.PageSize.ShouldBe(25);
            request.SortRequests.ShouldHaveSingleItem().PropertyName.ShouldBe("userName");
            request.FilterRequests.ShouldHaveSingleItem().PropertyName.ShouldBe("email");
        }

        [Fact]
        public void ToServiceRequest_NullLists_MapsToEmptyLists()
        {
            var request = new PaginationApiRequest().ToServiceRequest();

            request.PageNumber.ShouldBe(1);
            request.PageSize.ShouldBe(15);
            request.SortRequests.ShouldBeEmpty();
            request.FilterRequests.ShouldBeEmpty();
        }

        private static FilterApiRequest Filter(
            string propertyName,
            FilterApiRequest.Operator @operator = FilterApiRequest.Operator.Equal,
            string? value = "x",
            FilterApiRequest.Logic logic = FilterApiRequest.Logic.And,
            IEnumerable<FilterApiRequest>? nested = null)
        {
            return new FilterApiRequest(propertyName, @operator, value, logic, nested);
        }
    }
}

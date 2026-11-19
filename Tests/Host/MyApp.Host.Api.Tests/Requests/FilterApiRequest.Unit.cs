using MyApp.Host.Api.Requests;
using MyApp.Services.Abstractions.Requests;

namespace MyApp.Host.Api.Tests.Requests;

/// <summary>Its validation rules are child rules of <see cref="PaginationApiRequest.Validator"/>.</summary>
public sealed partial class FilterApiRequestTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Fact]
        public void ToServiceRequest_Filter_MapsEveryField()
        {
            var request = new FilterApiRequest("userName", FilterApiRequest.Operator.Contains, "jo", FilterApiRequest.Logic.Or)
                .ToServiceRequest();

            request.PropertyName.ShouldBe("userName");
            request.FilterOperator.ShouldBe(FilterServiceRequest.Operator.Contains);
            request.Value.ShouldBe("jo");
            request.FilterLogic.ShouldBe(FilterServiceRequest.Logic.Or);
            request.Filters.ShouldBeEmpty();
        }

        [Fact]
        public void ToServiceRequest_NullValue_KeepsNull()
        {
            new FilterApiRequest("email", FilterApiRequest.Operator.Equal, null, FilterApiRequest.Logic.And)
                .ToServiceRequest().Value.ShouldBeNull();
        }

        [Fact]
        public void ToServiceRequest_NestedFilters_MapsThemRecursively()
        {
            var request = new FilterApiRequest("userName", FilterApiRequest.Operator.Equal, "a", FilterApiRequest.Logic.And,
            [
                new FilterApiRequest("email", FilterApiRequest.Operator.EndWith, ".test", FilterApiRequest.Logic.Or,
                    [new FilterApiRequest("id", FilterApiRequest.Operator.NotEqual, "x", FilterApiRequest.Logic.And)])
            ]).ToServiceRequest();

            var nested = request.Filters.ShouldHaveSingleItem();
            nested.PropertyName.ShouldBe("email");
            nested.FilterOperator.ShouldBe(FilterServiceRequest.Operator.EndWith);
            nested.FilterLogic.ShouldBe(FilterServiceRequest.Logic.Or);
            nested.Filters.ShouldHaveSingleItem().FilterOperator.ShouldBe(FilterServiceRequest.Operator.NotEqual);
        }

        [Fact]
        public void ToServiceRequest_EveryOperatorAndLogic_MapsToTheSameServiceValue()
        {
            foreach (var value in Enum.GetValues<FilterApiRequest.Operator>())
                new FilterApiRequest("p", value, null, FilterApiRequest.Logic.And).ToServiceRequest()
                    .FilterOperator.ToString().ShouldBe(value.ToString());

            foreach (var value in Enum.GetValues<FilterApiRequest.Logic>())
                new FilterApiRequest("p", FilterApiRequest.Operator.Equal, null, value).ToServiceRequest()
                    .FilterLogic.ToString().ShouldBe(value.ToString());
        }
    }
}

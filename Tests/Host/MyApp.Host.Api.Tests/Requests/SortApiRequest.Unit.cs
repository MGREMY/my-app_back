using MyApp.Host.Api.Requests;

namespace MyApp.Host.Api.Tests.Requests;

/// <summary>Its validation rules are child rules of <see cref="PaginationApiRequest.Validator"/>.</summary>
public sealed partial class SortApiRequestTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ToServiceRequest_Sort_MapsEveryField(bool isDescending)
        {
            var request = new SortApiRequest("userName", isDescending).ToServiceRequest();

            request.PropertyName.ShouldBe("userName");
            request.IsDescending.ShouldBe(isDescending);
        }
    }
}

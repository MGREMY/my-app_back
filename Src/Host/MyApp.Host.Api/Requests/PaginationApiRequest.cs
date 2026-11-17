using FluentValidation;

namespace MyApp.Host.Api.Requests;

public sealed record PaginationApiRequest(
    int PageNumber = 1,
    int PageSize = 15,
    IEnumerable<SortApiRequest>? SortRequests = null,
    IEnumerable<FilterApiRequest>? FilterRequests = null,
    AdditionalFlagsApiRequest? AdditionalFlags = null)
{
    public Services.Abstractions.Requests.PaginationServiceRequest ToServiceRequest()
    {
        return new Services.Abstractions.Requests.PaginationServiceRequest(
            PageNumber,
            PageSize,
            SortRequests: SortRequests?.Select(x => x.ToServiceRequest()) ?? [],
            FilterRequests: FilterRequests?.Select(x => x.ToServiceRequest()) ?? [],
            AdditionalFlagsRequest: AdditionalFlags?.ToServiceRequest() ?? new(false));
    }

    public sealed class Validator : AbstractValidator<PaginationApiRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);
            RuleFor(x => x.PageSize)
                .GreaterThan(0);

            RuleFor(x => x.SortRequests)
                .ForEach(sortRequests => sortRequests.ChildRules(sort =>
                {
                    sort.RuleFor(x => x.PropertyName)
                        .NotEmpty();
                    sort.RuleFor(x => x.IsDescending)
                        .NotNull();
                }))
                .When(x => x.SortRequests?.Any() ?? false);

            RuleFor(x => x.FilterRequests)
                .NotEmpty()
                .ForEach(filterRequests => filterRequests.ChildRules(filter =>
                {
                    filter.RuleFor(x => x.PropertyName)
                        .NotEmpty();
                    filter.RuleFor(x => x.FilterOperator)
                        .NotNull();
                    filter.RuleFor(x => x.Value)
                        .NotNull();
                    filter.RuleFor(x => x.FilterLogic)
                        .NotNull();
                }))
                .When(x => x.FilterRequests?.Any() ?? false);
        }
    }
}
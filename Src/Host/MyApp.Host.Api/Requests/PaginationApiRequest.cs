using FluentValidation;
using MyApp.Services.Abstractions.Requests;

namespace MyApp.Host.Api.Requests;

public sealed record PaginationApiRequest(
    int PageNumber = 1,
    int PageSize = 15,
    IEnumerable<SortApiRequest>? SortRequests = null,
    IEnumerable<FilterApiRequest>? FilterRequests = null)
{
    public PaginationServiceRequest ToServiceRequest()
    {
        return new PaginationServiceRequest(
            PageNumber,
            PageSize,
            SortRequests?.Select(x => x.ToServiceRequest()) ?? [],
            FilterRequests?.Select(x => x.ToServiceRequest()) ?? []);
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
                        .IsInEnum();
                    // Value is nullable on purpose: a JSON null means "IS NULL" / "IS NOT NULL".
                    // Whether null is allowed depends on the property and the operator: checked by the generated code.
                    filter.RuleFor(x => x.FilterLogic)
                        .IsInEnum();
                }))
                .When(x => x.FilterRequests?.Any() ?? false);
        }
    }
}
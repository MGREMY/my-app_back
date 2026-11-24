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
            RuleFor(x => x.PageNumber).GreaterThan(0);
            RuleFor(x => x.PageSize).GreaterThan(0);
            RuleForEach(x => x.SortRequests).SetValidator(new SortApiRequest.Validator());
            RuleForEach(x => x.FilterRequests).SetValidator(new FilterApiRequest.Validator());
        }
    }
}
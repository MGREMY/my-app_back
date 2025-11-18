using FastEndpoints;
using FluentValidation;
using MyApp.Domain.Service.Contract.Dto.PaginationDto;

namespace MyApp.Host.Api.Dto.PaginationDto;

public record SortRequest(string PropertyName, bool IsDescending)
{
    public SortServiceRequest ToServiceRequest()
    {
        return new SortServiceRequest(PropertyName, IsDescending);
    }

    public class Validator : Validator<SortRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PropertyName)
                .NotEmpty();
            RuleFor(x => x.IsDescending)
                .NotNull();
        }
    }
}

public record PaginationRequest(int PageNumber = 1, int PageSize = 15, IEnumerable<SortRequest>? SortRequests = null)
{
    public PaginationServiceRequest ToServiceRequest()
    {
        return new PaginationServiceRequest(
            PageNumber,
            PageSize,
            SortServiceRequest: SortRequests?.Select(x => x.ToServiceRequest()) ?? []);
    }

    public class Validator : Validator<PaginationRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);
            RuleFor(x => x.PageSize)
                .GreaterThan(0);

            RuleFor(x => x.SortRequests)
                .NotEmpty()
                .When(x => x.SortRequests is not null);
        }
    }
}
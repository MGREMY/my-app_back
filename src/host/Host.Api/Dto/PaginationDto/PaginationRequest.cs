using Domain.Service.Contract.Dto.PaginationDto;
using FastEndpoints;
using FluentValidation;

namespace Host.Api.Dto.PaginationDto;

public record FilterRequest(
    string PropertyName,
    FilterRequest.Operator FilterOperator,
    string Value,
    FilterRequest.Logic FilterLogic,
    IEnumerable<FilterRequest>? Filters)
{
    public enum Operator
    {
        Equal = 0,
        NotEqual = 1,
        LessThan = 2,
        LessThanOrEqual = 3,
        GreaterThan = 4,
        GreaterThanOrEqual = 5,
        Contains = 6,
        NotContains = 7,
        StartWith = 8,
        EndWith = 9,
    }

    public enum Logic
    {
        And = 0,
        Or = 1,
    }

    public FilterServiceRequest ToServiceRequest()
    {
        return new FilterServiceRequest(
            PropertyName,
            (FilterServiceRequest.Operator)FilterOperator,
            Value,
            (FilterServiceRequest.Logic)FilterLogic,
            Filters?.Select(x => x.ToServiceRequest()) ?? []);
    }

    public class Validator : Validator<FilterRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PropertyName)
                .NotEmpty();
            RuleFor(x => x.FilterOperator)
                .NotNull();
            RuleFor(x => x.Value)
                .NotNull();
            RuleFor(x => x.FilterLogic)
                .NotNull();
            RuleFor(x => x.Filters)
                .NotEmpty()
                .When(x => x.Filters is not null);
        }
    }
}

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

public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 15,
    IEnumerable<SortRequest>? SortRequests = null,
    IEnumerable<FilterRequest>? FilterRequests = null)
{
    public PaginationServiceRequest ToServiceRequest()
    {
        return new PaginationServiceRequest(
            PageNumber,
            PageSize,
            SortServiceRequests: SortRequests?.Select(x => x.ToServiceRequest()) ?? [],
            FilterServiceRequests: FilterRequests?.Select(x => x.ToServiceRequest()) ?? []);
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
            
            RuleFor(x => x.FilterRequests)
                .NotEmpty()
                .When(x => x.FilterRequests is not null);
        }
    }
}
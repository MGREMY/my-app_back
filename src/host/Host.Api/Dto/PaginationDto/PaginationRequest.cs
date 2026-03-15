using System.Reflection;
using System.Text.Json;
using Domain.Service.Contract.Dto.PaginationDto;
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
}

public record SortRequest(string PropertyName, bool IsDescending)
{
    public SortServiceRequest ToServiceRequest()
    {
        return new SortServiceRequest(PropertyName, IsDescending);
    }
}

public record PaginationRequest(
    int PageNumber = 1,
    int PageSize = 15,
    IEnumerable<SortRequest>? SortRequests = null,
    IEnumerable<FilterRequest>? FilterRequests = null)
    : IBindableFromHttpContext<PaginationRequest>
{
    public PaginationServiceRequest ToServiceRequest()
    {
        return new PaginationServiceRequest(
            PageNumber,
            PageSize,
            SortServiceRequests: SortRequests?.Select(x => x.ToServiceRequest()) ?? [],
            FilterServiceRequests: FilterRequests?.Select(x => x.ToServiceRequest()) ?? []);
    }

    public static ValueTask<PaginationRequest?> BindAsync(HttpContext context, ParameterInfo _)
    {
        var pageNumber = int.TryParse(context.Request.Query["pageNumber"].FirstOrDefault(), out var pn) ? pn : 1;
        var pageSize = int.TryParse(context.Request.Query["pageSize"].FirstOrDefault(), out var ps) ? ps : 15;
        var sortRequestJson = context.Request.Query["sortRequests"].FirstOrDefault();
        var filterRequestJson = context.Request.Query["filterRequests"].FirstOrDefault();

        var sortRequest = string.IsNullOrWhiteSpace(sortRequestJson)
            ? null
            : JsonSerializer.Deserialize<SortRequest[]>(sortRequestJson, JsonSerializerOptions.Web);
        var filterRequest = string.IsNullOrWhiteSpace(filterRequestJson)
            ? null
            : JsonSerializer.Deserialize<FilterRequest[]>(filterRequestJson, JsonSerializerOptions.Web);

        return ValueTask.FromResult<PaginationRequest?>(new PaginationRequest(
            pageNumber,
            pageSize,
            sortRequest,
            filterRequest
        ));
    }

    public class Validator : AbstractValidator<PaginationRequest>
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
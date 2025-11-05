using FastEndpoints;
using FluentValidation;
using MyApp.Domain.Service.Contract.Dto.PaginationDto;

namespace MyApp.Host.Api.Dto.PaginationDto;

public record PaginationRequest(int PageNumber = 1, int PageSize = 15)
{
    public static bool TryParse(string? input, out PaginationRequest? output)
    {
        output = null;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var parts = input.Split(',');

        if (
            !int.TryParse(parts[0], out var pageNumber) ||
            !int.TryParse(parts[1], out var pageSize)
        )
        {
            return false;
        }

        output = new PaginationRequest(pageNumber, pageSize);

        return true;
    }

    public PaginationServiceRequest ToServiceRequest()
    {
        return new PaginationServiceRequest(PageNumber, PageSize);
    }

    public class Validator : Validator<PaginationRequest>
    {
        public Validator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0);
            RuleFor(x => x.PageSize)
                .GreaterThan(0);
        }
    }
}
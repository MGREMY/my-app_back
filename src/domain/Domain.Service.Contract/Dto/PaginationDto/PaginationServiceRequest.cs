namespace Domain.Service.Contract.Dto.PaginationDto;

public record SortServiceRequest(string PropertyName, bool IsDescending);

public record PaginationServiceRequest(int PageNumber, int PageSize, params IEnumerable<SortServiceRequest> SortServiceRequest);
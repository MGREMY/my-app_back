using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.Extensions;

public static class QueryableExtension
{
    extension<T>(IQueryable<T> query)
    {
        public PaginationServiceResponse<T> ToPagedResponse(
            int pageNumber,
            int pageSize,
            Func<int> totalCountAction)
        {
            var totalPages = (int)Math.Ceiling((double)totalCountAction() / pageSize);

            return new PaginationServiceResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray()
            };
        }

        public async Task<PaginationServiceResponse<T>> ToPagedResponseAsync(
            int pageNumber,
            int pageSize,
            Func<CancellationToken, Task<int>> totalCountFunc,
            CancellationToken ct)
        {
            var totalPages = (int)Math.Ceiling((double)await totalCountFunc(ct) / pageSize);

            return new PaginationServiceResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray()
            };
        }
    }
}
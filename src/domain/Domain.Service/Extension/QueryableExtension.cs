using System.Diagnostics.CodeAnalysis;
using Domain.Service.Contract.Dto;

namespace Domain.Service.Extension;

[ExcludeFromCodeCoverage]
public static class QueryableExtension
{
    extension<T>(IQueryable<T> query)
    {
        public PaginationResponse<T> ToPagedResponse(
            int pageNumber,
            int pageSize,
            Func<int> totalCountAction)
        {
            var totalPages = (int)Math.Ceiling((double)totalCountAction() / pageSize);

            return new PaginationResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray(),
            };
        }

        public async Task<PaginationResponse<T>> ToPagedResponseAsync(
            int pageNumber,
            int pageSize,
            Func<CancellationToken, Task<int>> totalCountFunc,
            CancellationToken ct)
        {
            var totalPages = (int)Math.Ceiling((double)await totalCountFunc(ct) / pageSize);

            return new PaginationResponse<T>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasPreviousPage = pageNumber > 1,
                HasNextPage = pageNumber < totalPages,
                TotalPages = totalPages,
                Data = query.ToArray(),
            };
        }
    }
}
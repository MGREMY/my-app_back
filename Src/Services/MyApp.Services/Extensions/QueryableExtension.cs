using Microsoft.EntityFrameworkCore;
using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.Extensions;

public static class QueryableExtension
{
    extension<T>(IQueryable<T> query)
    {
        /// <summary>
        ///     Materialize one page of <paramref name="query" /> and build the pagination metadata.
        /// </summary>
        /// <param name="pageNumber">Requested page, starting at 1.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="totalCountAction">Counts the filtered items, before paging.</param>
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

        /// <summary>
        ///     Materialize one page of <paramref name="query" /> asynchronously and build the pagination metadata.
        /// </summary>
        /// <param name="pageNumber">Requested page, starting at 1.</param>
        /// <param name="pageSize">Number of items per page.</param>
        /// <param name="totalCountFunc">Counts the filtered items, before paging (<c>countAsync</c> of <c>ProcessPaginationRequest</c>).</param>
        /// <param name="ct">Cancellation token, passed to the count and to the page query.</param>
        /// <remarks>
        ///     Only works on EF Core queries: the page is read with <c>ToArrayAsync</c>, which throws
        ///     <see cref="InvalidOperationException" /> when the provider is not asynchronous (for example
        ///     <c>list.AsQueryable()</c> in a unit test). Use <see cref="ToPagedResponse" /> for in-memory sources.
        /// </remarks>
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
                Data = await query.ToArrayAsync(ct)
            };
        }
    }
}
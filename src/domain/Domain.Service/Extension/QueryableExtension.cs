using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Domain.Model.Model.Interface;
using Domain.Service.Contract.Dto.PaginationDto;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Extension;

[ExcludeFromCodeCoverage]
public static class QueryableExtension
{
    extension<T, TKey>(IQueryable<T> query) where T : class, IBaseEntity<TKey>
    {
        public IQueryable<T> ApplyPagination(
            int pageNumber,
            int pageSize)
        {
            return query.ApplyPagination(pageNumber, pageSize, x => x.Id);
        }
    }

    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> ApplyPagination<TKey>(
            int pageNumber,
            int pageSize,
            Expression<Func<T, TKey>> keySelector)
        {
            return query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);
        }

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
                Data = query.ToArray(),
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
                Data = query.ToArray(),
            };
        }

        public IQueryable<T> ApplySorting(params IEnumerable<SortServiceRequest> sortRequests)
        {
            var hasFirstSort = false;

            foreach (var sortRequest in sortRequests)
            {
                if (!hasFirstSort)
                {
                    query = !sortRequest.IsDescending
                        ? query.OrderBy(x => EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()))
                        : query.OrderByDescending(x => EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()));

                    hasFirstSort = true;
                }
                else
                {
                    query = !sortRequest.IsDescending
                        ? (query as IOrderedQueryable<T>)!.ThenBy(x =>
                            EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()))
                        : (query as IOrderedQueryable<T>)!.ThenByDescending(x =>
                            EF.Property<T>(x!, sortRequest.PropertyName.Dehumanize()));
                }
            }

            return query;
        }
    }
}
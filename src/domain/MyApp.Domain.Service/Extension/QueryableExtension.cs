using System.Linq.Expressions;
using MyApp.Domain.Model.Model.Interface;
using MyApp.Domain.Service.Contract.Dto.PaginationDto;

namespace MyApp.Domain.Service.Extension;

public static class QueryableExtension
{
    public static IQueryable<T> ApplyPagination<T, TKey>(
        this IQueryable<T> set,
        int pageNumber,
        int pageSize)
        where T : class, IBaseEntity<TKey>
    {
        return set.ApplyPagination(pageNumber, pageSize, x => x.Id);
    }

    public static IQueryable<T> ApplyPagination<T, TKey>(
        this IQueryable<T> set,
        int pageNumber,
        int pageSize,
        Expression<Func<T, TKey>> keySelector) where T : class
    {
        return set
            .OrderBy(keySelector)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }

    public static PaginationServiceResponse<T> ToPagedResponse<T>(
        this IQueryable<T> query,
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

    public static async Task<PaginationServiceResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
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
}

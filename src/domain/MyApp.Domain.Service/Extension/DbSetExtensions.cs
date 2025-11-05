using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Model.Model.Interface;

namespace MyApp.Domain.Service.Extension;

public static class DbSetExtensions
{
    public static IQueryable<T> ApplyPagination<T, TKey>(
        this DbSet<T> set,
        int pageNumber,
        int pageSize)
        where T : class, IBaseEntity<TKey>
    {
        return set.ApplyPagination(pageNumber, pageSize, x => x.Id);
    }

    public static IQueryable<T> ApplyPagination<T, TKey>(
        this DbSet<T> set,
        int pageNumber,
        int pageSize,
        Expression<Func<T, TKey>> keySelector) where T : class
    {
        return set
            .OrderBy(keySelector)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
    }
}
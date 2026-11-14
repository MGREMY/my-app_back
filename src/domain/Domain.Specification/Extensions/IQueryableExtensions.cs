using System.Linq;

namespace Domain.Specification.Extensions
{
    /// <summary>
    ///     Provides query extensions that apply specifications to <see cref="IQueryable" /> sources.
    /// </summary>
    /// <remarks>
    ///     The extension targets <see cref="IQueryable{T}" /> so it works with any LINQ provider, not only
    ///     Entity Framework. The specification project stays persistence-agnostic; it only deals with
    ///     expression trees.
    ///     It is a regular extension method rather than an extension-block member, which keeps it usable
    ///     on older target frameworks such as netstandard2.0.
    /// </remarks>
    public static class IQueryableExtensions
    {
        /// <summary>
        ///     Filters the query with the predicate exposed by <paramref name="spec" />.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the source query.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="spec">The specification to apply.</param>
        /// <returns>
        ///     A new <see cref="IQueryable{T}" /> with the specification applied as a filter.
        /// </returns>
        /// <remarks>
        ///     This delegates to <c>Queryable.Where</c> with the specification's expression tree. Doing
        ///     so keeps execution deferred and lets the provider translate the tree to its native query
        ///     language instead of evaluating it in memory. The query type is preserved so additional
        ///     operators can be chained after <c>Spec</c>.
        /// </remarks>
        public static IQueryable<T> Spec<T>(this IQueryable<T> query, ISpecification<T> spec)
        {
            return query.Where(spec.Criteria);
        }
    }
}
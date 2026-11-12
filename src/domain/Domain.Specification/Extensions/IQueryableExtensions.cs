namespace Domain.Specification.Extensions;

public static class IQueryableExtensions
{
    extension<T>(IQueryable<T> query)
    {
        public IQueryable<T> Spec(ISpecification<T> spec)
        {
            return query.Where(spec.Criteria);
        }
    }
}
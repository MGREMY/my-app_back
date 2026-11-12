using System.Linq.Expressions;

namespace Domain.Specification;

public interface ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; }
}
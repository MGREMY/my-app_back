using System.Linq.Expressions;
using Domain.Specification.Helper;

namespace Domain.Specification;

public sealed class AndSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public Expression<Func<T, bool>> Criteria => ExpressionHelper.And(_left.Criteria, _right.Criteria);

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }
}

public sealed class OrSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public Expression<Func<T, bool>> Criteria => ExpressionHelper.Or(_left.Criteria, _right.Criteria);

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;
    }
}

public sealed class NotSpecification<T> : ISpecification<T>
{
    private readonly ISpecification<T> _specification;

    public Expression<Func<T, bool>> Criteria => Expression.Lambda<Func<T, bool>>(
        Expression.Not(_specification.Criteria.Body),
        _specification.Criteria.Parameters);

    public NotSpecification(ISpecification<T> specification)
    {
        _specification = specification;
    }
}
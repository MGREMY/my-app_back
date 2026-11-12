using System.Linq.Expressions;

namespace Domain.Specification.Helper;

public class ExpressionHelper
{
    public static Expression<Func<T, bool>> And<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.AndAlso);

    public static Expression<Func<T, bool>> Or<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
        => Combine(left, right, Expression.OrElse);

    private static Expression<Func<T, bool>> Combine<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression,
            BinaryExpression> merge)
    {
        var parameter = Expression.Parameter(typeof(T), "x");

        var leftBody = new ParameterReplacer(parameter)
            .Visit(left.Body);

        var rightBody = new ParameterReplacer(parameter)
            .Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(
            merge(leftBody!, rightBody!),
            parameter);
    }
}
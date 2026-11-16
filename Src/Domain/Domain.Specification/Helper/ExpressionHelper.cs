using System;
using System.Linq.Expressions;

namespace Domain.Specification.Helper
{
    /// <summary>
    ///     Provides helpers for merging specification predicates into single expression trees.
    /// </summary>
    /// <remarks>
    ///     Each specification exposes an independent lambda with its own <see cref="ParameterExpression" />.
    ///     A merged lambda can only bind a body to one parameter, so both operands must be rewritten to
    ///     reference the same parameter instance before their bodies can be combined.
    /// </remarks>
    internal class ExpressionHelper
    {
        /// <summary>
        ///     Combines two predicates with logical AND.
        /// </summary>
        /// <typeparam name="T">Type of the object the predicates evaluate.</typeparam>
        /// <param name="left">The left-hand predicate.</param>
        /// <param name="right">The right-hand predicate.</param>
        /// <returns>A predicate equivalent to <paramref name="left" /> AND <paramref name="right" />.</returns>
        /// <remarks>
        ///     <see cref="Expression.AndAlso(Expression, Expression)" /> is used rather than
        ///     <see cref="Expression.And(Expression, Expression)" /> so the predicate short-circuits,
        ///     matching the semantics of the C# <c>&amp;&amp;</c> operator and the expectations of query
        ///     providers.
        /// </remarks>
        public static Expression<Func<T, bool>> And<T>(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            return Combine(left, right, Expression.AndAlso);
        }

        /// <summary>
        ///     Combines two predicates with logical OR.
        /// </summary>
        /// <typeparam name="T">Type of the object the predicates evaluate.</typeparam>
        /// <param name="left">The left-hand predicate.</param>
        /// <param name="right">The right-hand predicate.</param>
        /// <returns>A predicate equivalent to <paramref name="left" /> OR <paramref name="right" />.</returns>
        /// <remarks>
        ///     <see cref="Expression.OrElse(Expression, Expression)" /> is used rather than
        ///     <see cref="Expression.Or(Expression, Expression)" /> so the predicate short-circuits,
        ///     matching the semantics of the C# <c>||</c> operator and the expectations of query providers.
        /// </remarks>
        public static Expression<Func<T, bool>> Or<T>(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right)
        {
            return Combine(left, right, Expression.OrElse);
        }

        /// <summary>
        ///     Rebinds both operands to a new shared parameter and merges their bodies.
        /// </summary>
        /// <typeparam name="T">Type of the object the predicates evaluate.</typeparam>
        /// <param name="left">The left-hand predicate.</param>
        /// <param name="right">The right-hand predicate.</param>
        /// <param name="merge">The binary operator used to combine both bodies.</param>
        /// <returns>A single predicate that references one shared parameter.</returns>
        /// <remarks>
        ///     A fresh parameter is created instead of reusing one from the operands so that neither
        ///     operand is mutated and both remain reusable in other compositions. Only each operand's
        ///     outer parameter (<c>Parameters[0]</c>) is replaced; parameters of nested lambdas inside the
        ///     bodies must stay untouched.
        /// </remarks>
        private static Expression<Func<T, bool>> Combine<T>(
            Expression<Func<T, bool>> left,
            Expression<Func<T, bool>> right,
            Func<Expression, Expression,
                BinaryExpression> merge)
        {
            // Shared parameter that both bodies will reference in the merged lambda.
            var parameter = Expression.Parameter(typeof(T), "x");

            // Rebinding is done per operand so the visitor knows exactly which outer parameter to
            // replace. A visitor without a source parameter would also rewrite nested lambda
            // parameters (Any/Where/etc.) and corrupt the query.
            var leftBody = new ParameterReplacer(left.Parameters[0], parameter)
                .Visit(left.Body);

            var rightBody = new ParameterReplacer(right.Parameters[0], parameter)
                .Visit(right.Body);

            // The input bodies are non-null, so the visitor never returns null here.
            return Expression.Lambda<Func<T, bool>>(
                merge(leftBody, rightBody),
                parameter);
        }
    }
}
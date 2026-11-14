using System;
using System.Linq.Expressions;

namespace Domain.Specification
{
    /// <summary>
    ///     Represents a reusable, composable predicate over <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">Type of the object the specification evaluates.</typeparam>
    /// <remarks>
    ///     A specification exposes its predicate as an expression tree rather than as a compiled delegate
    ///     or a boolean method. Keeping the tree intact lets persistence providers (for example EF Core)
    ///     translate the predicate to their native query language instead of evaluating it in memory.
    ///     Implementations should expose a pure predicate that depends only on <typeparamref name="T" />
    ///     and on state captured by the specification. The getter may be evaluated multiple times while
    ///     specifications are being composed, so it should not mutate state or return an unstable value.
    /// </remarks>
    public interface ISpecification<T>
    {
        /// <summary>
        ///     Gets the predicate that defines this specification.
        /// </summary>
        /// <value>
        ///     An expression that evaluates to <see langword="true" /> for objects matching the
        ///     specification and to <see langword="false" /> otherwise.
        /// </value>
        /// <remarks>
        ///     Each access may build a new expression tree. Code that needs both the body and the
        ///     parameters must read this property once and reuse the captured expression; reading it twice
        ///     can return two different tree instances whose <see cref="LambdaExpression.Parameters" /> are
        ///     distinct objects. Mixing them produces a lambda whose body references an unbound parameter.
        /// </remarks>
        Expression<Func<T, bool>> Criteria { get; }
    }
}
namespace Domain.Specification.Extensions
{
    /// <summary>
    ///     Provides fluent composition helpers for specifications.
    /// </summary>
    /// <remarks>
    ///     These are standard C# extension methods, so they appear as instance methods on
    ///     <see cref="ISpecification{T}" /> while the composition logic remains in a static class.
    ///     Using regular extension methods instead of newer extension-block syntax keeps the API usable
    ///     on older target frameworks such as netstandard2.0.
    ///     Composition returns <see cref="ISpecification{T}" /> rather than the concrete composition types.
    ///     This keeps call sites decoupled from the implementation and, more importantly, allows the
    ///     result of one composition to be composed again, which is what makes a declarative chain such as
    ///     <c>first.And(second).Not()</c> possible.
    ///     Composition builds lightweight wrapper objects only. The actual expression tree is produced
    ///     lazily when <see cref="ISpecification{T}.Criteria" /> is read.
    /// </remarks>
    public static class ISpecificationExtensions
    {
        /// <summary>
        ///     Combines the current specification with <paramref name="other" /> using logical AND.
        /// </summary>
        /// <typeparam name="T">Type of the object the specifications evaluate.</typeparam>
        /// <param name="specification">The left-hand specification.</param>
        /// <param name="other">The specification to combine with the current one.</param>
        /// <returns>A specification equivalent to <c>current AND other</c>.</returns>
        /// <remarks>
        ///     Both operands are evaluated left to right and the resulting predicate short-circuits,
        ///     mirroring the <c>&amp;&amp;</c> operator.
        /// </remarks>
        public static ISpecification<T> And<T>(this ISpecification<T> specification, ISpecification<T> other)
        {
            return new AndSpecification<T>(specification, other);
        }

        /// <summary>
        ///     Combines the current specification with <paramref name="other" /> using logical OR.
        /// </summary>
        /// <typeparam name="T">Type of the object the specifications evaluate.</typeparam>
        /// <param name="specification">The left-hand specification.</param>
        /// <param name="other">The specification to combine with the current one.</param>
        /// <returns>A specification equivalent to <c>current OR other</c>.</returns>
        /// <remarks>
        ///     Both operands are evaluated left to right and the resulting predicate short-circuits,
        ///     mirroring the <c>||</c> operator.
        /// </remarks>
        public static ISpecification<T> Or<T>(this ISpecification<T> specification, ISpecification<T> other)
        {
            return new OrSpecification<T>(specification, other);
        }

        /// <summary>
        ///     Negates the current specification.
        /// </summary>
        /// <typeparam name="T">Type of the object the specification evaluates.</typeparam>
        /// <param name="specification">The specification to negate.</param>
        /// <returns>A specification equivalent to <c>NOT current</c>.</returns>
        /// <remarks>
        ///     Because this is an extension method applied to the specification instance, it negates the
        ///     whole expression it is called on. Chaining it at the end of a composition negates the
        ///     entire composition: <c>a.And(b).Not()</c> means <c>NOT (a AND b)</c>.
        ///     To negate only one operand, call <c>Not()</c> on that operand before combining:
        ///     <c>a.And(b.Not())</c> means <c>a AND NOT b</c>.
        /// </remarks>
        public static ISpecification<T> Not<T>(this ISpecification<T> specification)
        {
            return new NotSpecification<T>(specification);
        }
    }
}
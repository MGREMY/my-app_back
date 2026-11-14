using System;
using System.Linq.Expressions;
using Domain.Specification.Helper;

namespace Domain.Specification
{
    /// <summary>
    ///     Specification that combines two specifications with a logical AND.
    /// </summary>
    /// <typeparam name="T">Type of the object the specification evaluates.</typeparam>
    /// <remarks>
    ///     The combined expression is not built in the constructor. Both operands are stored and the tree
    ///     is produced lazily whenever <see cref="Criteria" /> is read. This keeps composition cheap, allows
    ///     specifications to be nested arbitrarily deep, and leaves translation to the persistence provider
    ///     (for example EF Core) at query time.
    /// </remarks>
    public sealed class AndSpecification<T> : ISpecification<T>
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AndSpecification{T}" /> class.
        /// </summary>
        /// <param name="left">The left-hand specification.</param>
        /// <param name="right">The right-hand specification.</param>
        public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }

        /// <summary>
        ///     Gets the predicate representing <c>left AND right</c>.
        /// </summary>
        /// <remarks>
        ///     <see cref="ExpressionHelper.And{T}" /> rebuilds both bodies against a single shared
        ///     parameter, which is required because every specification authors its own parameter instance.
        /// </remarks>
        public Expression<Func<T, bool>> Criteria => ExpressionHelper.And(_left.Criteria, _right.Criteria);
    }

    /// <summary>
    ///     Specification that combines two specifications with a logical OR.
    /// </summary>
    /// <typeparam name="T">Type of the object the specification evaluates.</typeparam>
    /// <remarks>
    ///     Like <see cref="AndSpecification{T}" />, the tree is created lazily on each read of
    ///     <see cref="Criteria" /> so the operands remain untouched and can be reused elsewhere.
    /// </remarks>
    public sealed class OrSpecification<T> : ISpecification<T>
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OrSpecification{T}" /> class.
        /// </summary>
        /// <param name="left">The left-hand specification.</param>
        /// <param name="right">The right-hand specification.</param>
        public OrSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _left = left;
            _right = right;
        }

        /// <summary>
        ///     Gets the predicate representing <c>left OR right</c>.
        /// </summary>
        /// <remarks>
        ///     <see cref="ExpressionHelper.Or{T}" /> rebuilds both bodies against a single shared parameter,
        ///     which is required because every specification authors its own parameter instance.
        /// </remarks>
        public Expression<Func<T, bool>> Criteria => ExpressionHelper.Or(_left.Criteria, _right.Criteria);
    }

    /// <summary>
    ///     Specification that negates another specification.
    /// </summary>
    /// <typeparam name="T">Type of the object the specification evaluates.</typeparam>
    /// <remarks>
    ///     Negation only wraps the operand's body in <see cref="Expression.Not(Expression)" /> and reuses
    ///     the operand's parameter list. No parameter rebinding is needed because the result keeps the
    ///     same parameter as the operand.
    /// </remarks>
    public sealed class NotSpecification<T> : ISpecification<T>
    {
        private readonly ISpecification<T> _specification;

        /// <summary>
        ///     Initializes a new instance of the <see cref="NotSpecification{T}" /> class.
        /// </summary>
        /// <param name="specification">The specification to negate.</param>
        public NotSpecification(ISpecification<T> specification)
        {
            _specification = specification;
        }

        /// <summary>
        ///     Gets the predicate representing <c>NOT operand</c>.
        /// </summary>
        /// <remarks>
        ///     The operand's <see cref="ISpecification{T}.Criteria" /> is read exactly once and captured in a
        ///     local variable. Reading it twice can return two independently built expression trees, so the
        ///     body and the parameter list could reference different <see cref="ParameterExpression" />
        ///     instances. That would produce an invalid lambda whose body parameter is not bound by the
        ///     lambda declaration.
        /// </remarks>
        public Expression<Func<T, bool>> Criteria
        {
            get
            {
                // Read once: body and parameters must come from the same expression tree instance.
                var criteria = _specification.Criteria;
                return Expression.Lambda<Func<T, bool>>(
                    Expression.Not(criteria.Body),
                    criteria.Parameters);
            }
        }
    }
}
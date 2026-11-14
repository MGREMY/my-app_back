using System.Linq.Expressions;

namespace Domain.Specification
{
    /// <summary>
    ///     Rewrites the outer parameter of a specification's expression tree.
    /// </summary>
    /// <remarks>
    ///     Specifications are authored as independent lambdas, so each one owns its own
    ///     <see cref="ParameterExpression" /> (for example <c>x =&gt; x.Id == id</c>). To combine two
    ///     specifications into a single lambda, both bodies must reference the same parameter instance.
    ///     This visitor performs that rebinding.
    ///     Only the parameter instance passed as <c>from</c> is replaced. Nested lambdas
    ///     (for example <c>u =&gt; u.Orders.Any(o =&gt; ...)</c>) declare their own parameter instances;
    ///     replacing every parameter node would corrupt those inner lambdas.
    /// </remarks>
    internal class ParameterReplacer : ExpressionVisitor
    {
        private readonly ParameterExpression _from;
        private readonly ParameterExpression _to;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ParameterReplacer" /> class.
        /// </summary>
        /// <param name="from">The exact parameter instance to replace.</param>
        /// <param name="to">The parameter instance that should be referenced after rewriting.</param>
        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
        {
            _from = from;
            _to = to;
        }

        /// <summary>
        ///     Replaces the parameter node that is the same instance as the configured source parameter;
        ///     all other nodes are visited normally.
        /// </summary>
        /// <param name="node">The parameter node being visited.</param>
        /// <returns>The replacement parameter, or the original node if it is not the source.</returns>
        /// <remarks>
        ///     Identity is the correct comparison here: expression-tree parameters are identified by
        ///     instance, not by name or type. Nested lambdas can legitimately reuse the same name and type
        ///     and must remain distinct. <see cref="object.ReferenceEquals" /> makes that intent explicit.
        /// </remarks>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return ReferenceEquals(node, _from) ? _to : base.VisitParameter(node);
        }
    }
}
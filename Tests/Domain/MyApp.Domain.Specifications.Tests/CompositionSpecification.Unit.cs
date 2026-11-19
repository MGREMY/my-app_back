using System.Linq.Expressions;
using MyApp.Domain.Specifications.Tests.TestSpecifications;

namespace MyApp.Domain.Specifications.Tests;

public sealed partial class CompositionSpecificationTests
{
    [UnitTest]
    public sealed class Unit
    {
        private static readonly ISpecification<Item> Positive = new ItemIsPositiveSpec();
        private static readonly ISpecification<Item> Even = new ItemIsEvenSpec();

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, false)]
        [InlineData(-2, false)]
        [InlineData(-1, false)]
        public void AndCriteria_TwoSpecifications_MatchesWhenBothMatch(int value, bool expected)
        {
            var spec = new AndSpecification<Item>(Positive, Even);

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData(2, true)]
        [InlineData(1, true)]
        [InlineData(-2, true)]
        [InlineData(-1, false)]
        public void OrCriteria_TwoSpecifications_MatchesWhenEitherMatches(int value, bool expected)
        {
            var spec = new OrSpecification<Item>(Positive, Even);

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(-1, true)]
        public void NotCriteria_Specification_InvertsResult(int value, bool expected)
        {
            var spec = new NotSpecification<Item>(Positive);

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }

        [Fact]
        public void AndCriteria_TwoSpecifications_UsesShortCircuitAndAlso()
        {
            var spec = new AndSpecification<Item>(Positive, Even);

            var criteria = spec.Criteria;

            criteria.Body.NodeType.ShouldBe(ExpressionType.AndAlso);
        }

        [Fact]
        public void OrCriteria_TwoSpecifications_UsesShortCircuitOrElse()
        {
            var spec = new OrSpecification<Item>(Positive, Even);

            var criteria = spec.Criteria;

            criteria.Body.NodeType.ShouldBe(ExpressionType.OrElse);
        }

        [Fact]
        public void AndCriteria_LeftFalse_DoesNotEvaluateRight()
        {
            var spec = new AndSpecification<Item>(new ItemHasNameSpec(), new ItemHasLongNameSpec());

            var result = spec.Criteria.Compile()(new Item(1, Name: null));

            result.ShouldBeFalse();
        }

        [Fact]
        public void OrCriteria_LeftTrue_DoesNotEvaluateRight()
        {
            var spec = new OrSpecification<Item>(new NotSpecification<Item>(new ItemHasNameSpec()), new ItemHasLongNameSpec());

            var result = spec.Criteria.Compile()(new Item(1, Name: null));

            result.ShouldBeTrue();
        }

        [Fact]
        public void AndCriteria_TwoSpecifications_BindsBothBodiesToSingleParameter()
        {
            var spec = new AndSpecification<Item>(Positive, Even);

            var criteria = spec.Criteria;

            var parameter = criteria.Parameters.ShouldHaveSingleItem();
            CollectParameters(criteria.Body).ShouldAllBe(x => ReferenceEquals(x, parameter));
        }

        [Fact]
        public void AndCriteria_OperandWithNestedLambda_KeepsNestedParameter()
        {
            var spec = new AndSpecification<Item>(Positive, new ItemHasPositiveChildSpec());

            var predicate = spec.Criteria.Compile();

            predicate(new Item(1, Children: [-1, 2])).ShouldBeTrue();
            predicate(new Item(1, Children: [-1, -2])).ShouldBeFalse();
            predicate(new Item(-1, Children: [2])).ShouldBeFalse();
        }

        [Fact]
        public void NotCriteria_ComposedSpecification_ProducesBoundLambda()
        {
            var spec = new NotSpecification<Item>(new AndSpecification<Item>(Positive, Even));

            var criteria = spec.Criteria;

            var parameter = criteria.Parameters.ShouldHaveSingleItem();
            CollectParameters(criteria.Body).ShouldAllBe(x => ReferenceEquals(x, parameter));
            criteria.Compile()(new Item(2)).ShouldBeFalse();
        }

        [Fact]
        public void AndCriteria_OperandsReusedInSeveralCompositions_LeavesOperandsUnchanged()
        {
            var and = new AndSpecification<Item>(Positive, Even);
            var or = new OrSpecification<Item>(Positive, Even);

            var andResult = and.Criteria.Compile()(new Item(1));
            var orResult = or.Criteria.Compile()(new Item(1));

            andResult.ShouldBeFalse();
            orResult.ShouldBeTrue();
            Positive.Criteria.Compile()(new Item(1)).ShouldBeTrue();
            Even.Criteria.Compile()(new Item(1)).ShouldBeFalse();
        }

        private static List<ParameterExpression> CollectParameters(Expression expression)
        {
            var collector = new ParameterCollector();
            collector.Visit(expression);

            return collector.Parameters;
        }

        private sealed class ParameterCollector : ExpressionVisitor
        {
            public List<ParameterExpression> Parameters { get; } = [];

            protected override Expression VisitParameter(ParameterExpression node)
            {
                Parameters.Add(node);

                return base.VisitParameter(node);
            }
        }
    }
}

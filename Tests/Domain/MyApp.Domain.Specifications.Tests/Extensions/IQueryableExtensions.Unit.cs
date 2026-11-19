using System.Linq.Expressions;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.Tests.TestSpecifications;

namespace MyApp.Domain.Specifications.Tests.Extensions;

public sealed partial class IQueryableExtensionsTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly Item[] Items = [new(-2), new(-1), new(1), new(2), new(4)];

        [Fact]
        public void Spec_Specification_FiltersMatchingElements()
        {
            var result = Items.AsQueryable().Spec(new ItemIsPositiveSpec()).ToArray();

            result.Select(x => x.Value).ShouldBe([1, 2, 4]);
        }

        [Fact]
        public void Spec_ComposedSpecification_FiltersMatchingElements()
        {
            var result = Items.AsQueryable().Spec(new ItemIsPositiveSpec().And(new ItemIsEvenSpec().Not())).ToArray();

            result.Select(x => x.Value).ShouldBe([1]);
        }

        [Fact]
        public void Spec_Query_AddsWhereWithSpecificationTreeWithoutExecuting()
        {
            var source = Items.AsQueryable();

            var query = source.Spec(new ItemIsPositiveSpec());

            var call = query.Expression.ShouldBeAssignableTo<MethodCallExpression>()!;
            call.Method.Name.ShouldBe(nameof(Queryable.Where));
            call.Method.DeclaringType.ShouldBe(typeof(Queryable));
            call.Arguments[0].ShouldBeSameAs(source.Expression);
            var predicate = call.Arguments[1].ShouldBeOfType<UnaryExpression>().Operand.ShouldBeAssignableTo<Expression<Func<Item, bool>>>()!;
            predicate.Compile()(new Item(1)).ShouldBeTrue();
        }

        [Fact]
        public void Spec_Chained_AppliesEverySpecification()
        {
            var result = Items.AsQueryable()
                .Spec(new ItemIsPositiveSpec())
                .Spec(new ItemIsEvenSpec())
                .ToArray();

            result.Select(x => x.Value).ShouldBe([2, 4]);
        }
    }
}

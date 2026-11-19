using System.Linq.Expressions;
using MyApp.Domain.Specifications.Tests.TestSpecifications;

namespace MyApp.Domain.Specifications.Tests;

public sealed partial class ParameterReplacerTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void Visit_SourceParameter_ReplacesIt()
        {
            Expression<Func<Item, bool>> lambda = x => x.Value > 0;
            var target = Expression.Parameter(typeof(Item), "y");

            var body = CreateReplacer(lambda.Parameters[0], target).Visit(lambda.Body);

            Expression.Lambda<Func<Item, bool>>(body, target).Compile()(new Item(1)).ShouldBeTrue();
        }

        [Fact]
        public void Visit_NestedLambdaWithSameNameAndType_KeepsNestedParameter()
        {
            Expression<Func<Item, bool>> lambda = x => x.Children!.Select(_ => x).Any(x => x.Value > 0);
            var source = lambda.Parameters[0];
            var target = Expression.Parameter(typeof(Item), "x");

            var body = CreateReplacer(source, target).Visit(lambda.Body);

            var predicate = Expression.Lambda<Func<Item, bool>>(body, target).Compile();
            predicate(new Item(1, Children: [0])).ShouldBeTrue();
            predicate(new Item(-1, Children: [0])).ShouldBeFalse();
        }

        [Fact]
        public void Visit_OtherParameter_LeavesItUntouched()
        {
            var source = Expression.Parameter(typeof(Item), "x");
            var other = Expression.Parameter(typeof(Item), "x");
            var target = Expression.Parameter(typeof(Item), "y");

            var result = CreateReplacer(source, target).Visit(other);

            result.ShouldBeSameAs(other);
        }

        // ParameterReplacer is internal and Src exposes no InternalsVisibleTo: create it through reflection.
        private static ExpressionVisitor CreateReplacer(ParameterExpression from, ParameterExpression to)
        {
            var type = typeof(ISpecification<>).Assembly.GetType("MyApp.Domain.Specifications.ParameterReplacer", throwOnError: true)!;

            return (ExpressionVisitor)Activator.CreateInstance(type, from, to)!;
        }
    }
}

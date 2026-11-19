using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.Tests.TestSpecifications;

namespace MyApp.Domain.Specifications.Tests.Extensions;

public sealed partial class ISpecificationExtensionsTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        private static readonly ISpecification<Item> Positive = new ItemIsPositiveSpec();
        private static readonly ISpecification<Item> Even = new ItemIsEvenSpec();

        [Fact]
        public void And_TwoSpecifications_ReturnsAndSpecification()
        {
            var result = Positive.And(Even);

            result.ShouldBeOfType<AndSpecification<Item>>();
        }

        [Fact]
        public void Or_TwoSpecifications_ReturnsOrSpecification()
        {
            var result = Positive.Or(Even);

            result.ShouldBeOfType<OrSpecification<Item>>();
        }

        [Fact]
        public void Not_Specification_ReturnsNotSpecification()
        {
            var result = Positive.Not();

            result.ShouldBeOfType<NotSpecification<Item>>();
        }

        [Theory]
        [InlineData(2, false)] // NOT (true AND true)
        [InlineData(1, true)] // NOT (true AND false)
        [InlineData(-2, true)] // NOT (false AND true)
        [InlineData(-1, true)] // NOT (false AND false)
        public void Not_AfterComposition_NegatesWholeComposition(int value, bool expected)
        {
            var spec = Positive.And(Even).Not();

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData(2, false)] // true AND NOT true
        [InlineData(1, true)] // true AND NOT false
        [InlineData(-2, false)] // false AND NOT true
        [InlineData(-1, false)] // false AND NOT false
        public void Not_OnOperand_NegatesOnlyThatOperand(int value, bool expected)
        {
            var spec = Positive.And(Even.Not());

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }

        [Theory]
        [InlineData(3, true)]
        [InlineData(2, false)]
        [InlineData(-3, true)]
        [InlineData(-2, true)]
        public void Or_ChainedWithNot_ComposesLeftToRight(int value, bool expected)
        {
            // (NOT positive) OR (positive AND NOT even)
            var spec = Positive.Not().Or(Positive.And(Even.Not()));

            var result = spec.Criteria.Compile()(new Item(value));

            result.ShouldBe(expected);
        }
    }
}

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.Tests.TestSpecifications;
using MyApp.Domain.Specifications.UserSpecifications;

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
        public void Spec_ComposedSpecificationsOnNpgsql_TranslatesToSql()
        {
            // The model is built by the Npgsql provider and the query translated without opening a connection.
            using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql("Host=localhost;Database=unused")
                .Options);
            var spec = new UserHasIdSpec(Guid.NewGuid())
                .And(new UserHasAuthIdSpec("a").Or(new UserHasAuthIdSpec("b").Not()))
                .Not();

            var sql = context.Users.Spec(spec).ToQueryString();

            // NOT (id AND (a OR NOT b)), pushed down by EF Core, next to the soft deletion filter, with SQL parameters.
            sql.ShouldEndWith(
                "WHERE NOT (u.is_deleted) AND (u.id <> @_id OR (u.auth_id <> @_authId AND u.auth_id = @_authId2))");
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

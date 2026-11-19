using Microsoft.CodeAnalysis;
using MyApp.Services.Generators.Tests.TestInfrastructure;

namespace MyApp.Services.Generators.Tests.Diagnostics;

/// <summary>Diagnostics reported by the generator: PAG001 (name collision) and PAG002 (renamed extensions class).</summary>
public sealed partial class PaginationDiagnosticTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void Generator_CleanModel_ReportsNoDiagnosticAndCompiles()
        {
            var result = GeneratorRunner.Run(
                """
                namespace Sample;

                public enum Kind { A, B }

                public class Role
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; } = "";
                }

                public class Post
                {
                    public Guid Id { get; set; }
                    public int? Likes { get; set; }
                    public virtual User Author { get; set; } = null!;
                }

                public class User
                {
                    public Guid Id { get; set; }
                    public string UserName { get; set; } = "";
                    public Kind Kind { get; set; }
                    public List<string> Tags { get; set; } = [];
                    public byte[]? Avatar { get; set; }
                    public TimeSpan Timeout { get; set; }
                    public virtual Role Role { get; set; } = null!;
                    public virtual User? Manager { get; set; }
                    public virtual ICollection<Post> Posts { get; set; } = [];
                }

                [MyApp.Services.PaginationHandlerFor<User>]
                public sealed class UserHandler;
                """);

            result.Diagnostics.ShouldBeEmpty();
            result.CompilationErrors.ShouldBeEmpty();
            result.GeneratedSource.ShouldContain("public static class UserPaginationExtensions");
        }

        [Theory]
        [InlineData("UserName", "User_Name", "username")]
        [InlineData("Name", "NAME", "name")]
        public void Generator_NormalizedNameCollision_ReportsPag001OnTheLostProperty(string first, string second, string label)
        {
            var result = GeneratorRunner.Run(
                $$"""
                  namespace Sample;

                  public class User
                  {
                      public Guid Id { get; set; }
                      public string {{first}} { get; set; } = "";
                      public string {{second}} { get; set; } = "";
                  }

                  [MyApp.Services.PaginationHandlerFor<User>]
                  public sealed class UserHandler;
                  """);

            var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
            diagnostic.Id.ShouldBe("PAG001");
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().ShouldContain($"'{first}' and '{second}' of 'Sample.User' have the same normalized name '{label}'");
            diagnostic.Location.SourceTree!.ToString().Substring(diagnostic.Location.SourceSpan.Start, second.Length).ShouldBe(second);
            result.CompilationErrors.ShouldBeEmpty();
        }

        [Fact]
        public void Generator_CollisionInNavigatedEntity_ReportsPag001Once()
        {
            var result = GeneratorRunner.Run(
                """
                namespace Sample;

                public class Role
                {
                    public Guid Id { get; set; }
                    public string Label { get; set; } = "";
                    public string _label { get; set; } = "";
                }

                public class User
                {
                    public Guid Id { get; set; }
                    public virtual Role Role { get; set; } = null!;
                    public virtual Role? BackupRole { get; set; }
                }

                public class Group
                {
                    public Guid Id { get; set; }
                    public virtual ICollection<Role> Roles { get; set; } = [];
                }

                [MyApp.Services.PaginationHandlerFor<User>]
                public sealed class UserHandler;

                [MyApp.Services.PaginationHandlerFor<Group>]
                public sealed class GroupHandler;
                """);

            result.Diagnostics.ShouldHaveSingleItem().Id.ShouldBe("PAG001");
            result.CompilationErrors.ShouldBeEmpty();
        }

        [Fact]
        public void Generator_SameEntityNameInTwoNamespaces_ReportsPag002AndSuffixesTheSecondExtensions()
        {
            var result = GeneratorRunner.Run(
                """
                namespace Billing
                {
                    public class Invoice
                    {
                        public Guid Id { get; set; }
                        public decimal Total { get; set; }
                    }

                    [MyApp.Services.PaginationHandlerFor<Invoice>]
                    public sealed class InvoiceHandler;
                }

                namespace Sales
                {
                    public class Invoice
                    {
                        public Guid Id { get; set; }
                        public string Number { get; set; } = "";
                    }

                    [MyApp.Services.PaginationHandlerFor<Invoice>]
                    public sealed class InvoiceHandler;
                }
                """);

            var diagnostic = result.Diagnostics.ShouldHaveSingleItem();
            diagnostic.Id.ShouldBe("PAG002");
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Info);
            diagnostic.GetMessage().ShouldBe(
                "Paginated entity 'Sales.Invoice' has the same name as another paginated entity: its extensions are generated as 'Invoice2PaginationExtensions'");
            result.CompilationErrors.ShouldBeEmpty();
            result.GeneratedSource.ShouldContain("public static class InvoicePaginationExtensions");
            result.GeneratedSource.ShouldContain("public static class Invoice2PaginationExtensions");
        }

        [Fact]
        public void Generator_SameEntityPaginatedTwice_GeneratesItOnce()
        {
            var result = GeneratorRunner.Run(
                """
                namespace Sample;

                public class User
                {
                    public Guid Id { get; set; }
                }

                [MyApp.Services.PaginationHandlerFor<User>]
                public sealed class UserGet;

                [MyApp.Services.PaginationHandlerFor<User>]
                public sealed class UserExport;
                """);

            result.Diagnostics.ShouldBeEmpty();
            result.CompilationErrors.ShouldBeEmpty();
        }
    }
}

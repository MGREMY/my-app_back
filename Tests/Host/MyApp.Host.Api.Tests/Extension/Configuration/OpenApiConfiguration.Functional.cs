using System.Net;
using System.Text.Json;
using MyApp.Host.Api.Tests.TestInfrastructure;
using MyApp.Tests.Shared.Containers;

namespace MyApp.Host.Api.Tests.Extension.Configuration;

public sealed partial class OpenApiConfigurationTests
{
    [Trait(TestCategory.Name, TestCategory.Functional)]
    [Collection(DatabaseCollection.Name)]
    public sealed class Functional(ContainersFixture containers, ApiFactory factory) : ApiFunctionalTest(containers, factory)
    {
        private const string NotDeclared = "Bug: status code not declared in the OpenAPI document: ";

        private const string ProblemDropped = "Bug: status code missing from the OpenAPI document, the declaration is dropped: ";

        [Theory]
        [InlineData("/openapi/v1.json")]
        [InlineData("/scalar")]
        public async Task Documentation_OutsideDevelopment_IsNotMapped(string route)
        {
            using var response = await CreateClient().GetAsync(route, Ct);

            response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        }

        // Every status code an endpoint can return is declared in its OpenAPI responses (.Produces...), read from the
        // document served in Development.
        [Theory]
        [InlineData("GET", "/users/{id}", 200)]
        [InlineData("GET", "/users/{id}", 400, Skip = NotDeclared + "GET /users/{id} returns 400 for a non-GUID id.")]
        [InlineData("GET", "/users/{id}", 401, Skip = NotDeclared + "GET /users/{id} requires authorization (401).")]
        [InlineData("GET", "/users/{id}", 404, Skip = ProblemDropped + "Produces<ProblemHttpResult>(404) on GET /users/{id}.")]
        [InlineData("DELETE", "/users/{id}", 204)]
        [InlineData("DELETE", "/users/{id}", 400, Skip = ProblemDropped + "Produces<ProblemHttpResult>(400) on DELETE /users/{id}.")]
        [InlineData("DELETE", "/users/{id}", 401, Skip = NotDeclared + "DELETE /users/{id} requires authorization (401).")]
        [InlineData("DELETE", "/users/{id}", 403, Skip = NotDeclared + "DELETE /users/{id} is admin-only (403).")]
        [InlineData("DELETE", "/users/{id}", 404, Skip = ProblemDropped + "Produces<ProblemHttpResult>(404) on DELETE /users/{id}.")]
        [InlineData("POST", "/auth/sync-user", 204)]
        [InlineData("POST", "/auth/sync-user", 400, Skip = ProblemDropped + "Produces<ProblemHttpResult>(400) on POST /auth/sync-user.")]
        [InlineData("POST", "/auth/sync-user", 401, Skip = NotDeclared + "POST /auth/sync-user requires authorization (401).")]
        [InlineData("POST", "/auth/sync-user", 403, Skip = NotDeclared + "POST /auth/sync-user returns 403 for a deleted user.")]
        [InlineData("POST", "/auth/sync-user", 409, Skip = NotDeclared + "POST /auth/sync-user returns 409 to the loser of concurrent first syncs.")]
        [InlineData("GET", "/misc/anti-forgery-token", 204)]
        public async Task Document_Operation_DeclaresStatusCode(string method, string path, int statusCode)
        {
            using var document = JsonDocument.Parse(await Factory.GetOpenApiDocumentAsync());

            var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method.ToLowerInvariant());

            operation.GetProperty("responses").EnumerateObject().Select(x => x.Name).ShouldContain(statusCode.ToString());
        }

        [Theory]
        [InlineData(200, Skip = "Bug: QUERY /users is missing from the OpenAPI document (the 3.1 document has no QUERY "
                                + "operation): the paginated list is not documented at all.")]
        [InlineData(400, Skip = "Bug: QUERY /users is missing from the OpenAPI document (the 3.1 document has no QUERY "
                                + "operation): the paginated list is not documented at all.")]
        [InlineData(401, Skip = "Bug: QUERY /users is missing from the OpenAPI document (the 3.1 document has no QUERY "
                                + "operation): the paginated list is not documented at all.")]
        public async Task Document_QueryUsers_DeclaresStatusCode(int statusCode)
        {
            using var document = JsonDocument.Parse(await Factory.GetOpenApiDocumentAsync());

            var pathItem = document.RootElement.GetProperty("paths").GetProperty("/users");
            var operation = pathItem.EnumerateObject()
                .Where(x => x.Name.Equals("query", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Value)
                .Concat(pathItem.TryGetProperty("additionalOperations", out var additional)
                    ? additional.EnumerateObject().Where(x => x.Name.Equals("QUERY", StringComparison.OrdinalIgnoreCase)).Select(x => x.Value)
                    : [])
                .ShouldHaveSingleItem();

            operation.GetProperty("responses").EnumerateObject().Select(x => x.Name).ShouldContain(statusCode.ToString());
        }
    }
}

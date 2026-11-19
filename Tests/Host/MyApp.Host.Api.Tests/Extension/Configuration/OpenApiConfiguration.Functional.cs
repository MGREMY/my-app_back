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
        // Bug: the Produces<ProblemHttpResult>(4xx) declarations are dropped from the document, and 400 (non-GUID id),
        // 401, 403 and 409 are never declared.
        [Theory]
        [InlineData("GET", "/users/{id}", 200)]
        [InlineData("GET", "/users/{id}", 400)]
        [InlineData("GET", "/users/{id}", 401)]
        [InlineData("GET", "/users/{id}", 404)]
        [InlineData("DELETE", "/users/{id}", 204)]
        [InlineData("DELETE", "/users/{id}", 400)]
        [InlineData("DELETE", "/users/{id}", 401)]
        [InlineData("DELETE", "/users/{id}", 403)]
        [InlineData("DELETE", "/users/{id}", 404)]
        [InlineData("POST", "/auth/sync-user", 204)]
        [InlineData("POST", "/auth/sync-user", 400)]
        [InlineData("POST", "/auth/sync-user", 401)]
        [InlineData("POST", "/auth/sync-user", 403)]
        [InlineData("POST", "/auth/sync-user", 409)]
        [InlineData("GET", "/misc/anti-forgery-token", 204)]
        public async Task Document_Operation_DeclaresStatusCode(string method, string path, int statusCode)
        {
            using var document = JsonDocument.Parse(await Factory.GetOpenApiDocumentAsync());

            var operation = document.RootElement.GetProperty("paths").GetProperty(path).GetProperty(method.ToLowerInvariant());

            operation.GetProperty("responses").EnumerateObject().Select(x => x.Name).ShouldContain(statusCode.ToString());
        }

        // Bug: QUERY /users is missing from the OpenAPI document (the 3.1 document has no QUERY operation): the
        // paginated list is not documented at all.
        [Theory]
        [InlineData(200)]
        [InlineData(400)]
        [InlineData(401)]
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

using System.Net.Http.Json;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>Request options other than identity, expressed as intents (culture, API version, QUERY method).</summary>
public static class ApiClientExtensions
{
    private const string ApiVersionHeader = "X-Api-Version";

    public static readonly HttpMethod Query = new("QUERY");

    extension(HttpClient client)
    {
        /// <summary>Asks for messages in <paramref name="culture"/> (<c>Accept-Language</c>).</summary>
        public HttpClient WithCulture(string culture)
        {
            client.DefaultRequestHeaders.AcceptLanguage.Clear();
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd(culture);

            return client;
        }

        /// <summary>Asks for <paramref name="version"/> through the <c>X-Api-Version</c> header.</summary>
        public HttpClient WithApiVersionHeader(string version)
        {
            client.DefaultRequestHeaders.Remove(ApiVersionHeader);
            client.DefaultRequestHeaders.Add(ApiVersionHeader, version);

            return client;
        }

        /// <summary>Sends <paramref name="body"/> as JSON with the <c>QUERY</c> method (paginated lists).</summary>
        public Task<HttpResponseMessage> QueryAsJsonAsync<T>(string uri, T body, CancellationToken ct = default)
        {
            return client.SendAsync(new HttpRequestMessage(Query, uri) { Content = JsonContent.Create(body) }, ct);
        }
    }
}

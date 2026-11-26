namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>Request options other than identity, expressed as intents (accepted media types, API version, QUERY method).</summary>
public static class ApiClientExtensions
{
    private const string ApiVersionHeader = "X-Api-Version";

    public static readonly HttpMethod Query = new("QUERY");

    extension(HttpClient client)
    {
        /// <summary>Sends <paramref name="accept"/> as <c>Accept</c> header; <c>null</c> sends none (the default).</summary>
        /// <param name="accept">Media types accepted by the client (<c>application/xml</c>...).</param>
        public HttpClient WithAccept(string? accept)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            if (accept is not null)
                client.DefaultRequestHeaders.Accept.ParseAdd(accept);

            return client;
        }

        /// <summary>Asks for <paramref name="version"/> through the <c>X-Api-Version</c> header.</summary>
        /// <param name="version">API version sent in <c>X-Api-Version</c> (<c>1.0</c>...).</param>
        public HttpClient WithApiVersionHeader(string version)
        {
            client.DefaultRequestHeaders.Remove(ApiVersionHeader);
            client.DefaultRequestHeaders.Add(ApiVersionHeader, version);

            return client;
        }

        /// <summary>Sends <paramref name="body"/> as JSON with the <c>QUERY</c> method (paginated lists).</summary>
        /// <typeparam name="T">Type of the request body.</typeparam>
        /// <param name="uri">Relative URI of the paginated list.</param>
        /// <param name="body">Pagination request, sent as JSON.</param>
        /// <param name="ct">Cancellation token of the request.</param>
        public Task<HttpResponseMessage> QueryAsJsonAsync<T>(string uri, T body, CancellationToken ct = default)
        {
            return client.SendAsync(new HttpRequestMessage(Query, uri) { Content = JsonContent.Create(body) }, ct);
        }
    }
}

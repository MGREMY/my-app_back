using MyApp.Tests.Shared.Data;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>User sent by <see cref="ApiClientAuthExtensions.As"/>.</summary>
/// <param name="AuthId"><c>sub</c> claim.</param>
/// <param name="UserName">Keycloak username (<c>preferred_username</c> claim); <c>null</c> omits the claim.</param>
/// <param name="Email"><c>email</c> claim; <c>null</c> omits the claim.</param>
/// <param name="Roles">Role claims; empty for a user without role.</param>
/// <param name="DisplayName">Full name (<c>name</c> claim); defaults to <paramref name="UserName"/>.</param>
public sealed record TestUser(
    string AuthId,
    string? UserName,
    string? Email,
    IReadOnlyCollection<string> Roles,
    string? DisplayName = null)
{
    public static TestUser Create(string authId, IReadOnlyCollection<string>? roles = null, string? userName = null, string? email = null)
    {
        return new TestUser(authId, userName ?? $"user-{authId}", email ?? $"{authId}@example.test", roles ?? []);
    }
}

/// <summary>
/// The only place deciding who calls the API (and how the antiforgery token is sent): tests express an intent
/// (<c>client.AsUser()</c>, <c>client.AsAdmin()</c>...) and never build tokens, cookies, claims or headers themselves.
/// </summary>
/// <remarks>
/// BFF switch: when authentication moves behind the BFF, only this class, <see cref="TestAuthHandler"/> and
/// <see cref="ApiFactory"/> should change, not the test bodies.
/// </remarks>
public static class ApiClientAuthExtensions
{
    private const string AntiforgeryCookieName = "XSRF-TOKEN";
    private const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    private static readonly string[] IdentityHeaders =
    [
        TestAuthHandler.AuthIdHeader, TestAuthHandler.UserNameHeader, TestAuthHandler.DisplayNameHeader,
        TestAuthHandler.EmailHeader, TestAuthHandler.RolesHeader
    ];

    extension(HttpClient client)
    {
        /// <summary>Calls the API without identity.</summary>
        public HttpClient AsAnonymous()
        {
            foreach (var header in IdentityHeaders)
                client.DefaultRequestHeaders.Remove(header);

            return client;
        }

        /// <summary>Calls the API as an authenticated user without role.</summary>
        /// <param name="authId"><c>sub</c> claim of the caller.</param>
        /// <param name="userName"><c>preferred_username</c> claim; <c>user-&lt;authId&gt;</c> by default.</param>
        /// <param name="email"><c>email</c> claim; <c>&lt;authId&gt;@example.test</c> by default.</param>
        public HttpClient AsUser(string authId = "user-1", string? userName = null, string? email = null)
        {
            return client.As(TestUser.Create(authId, userName: userName, email: email));
        }

        /// <summary>Calls the API as a user with the admin role (<see cref="ApiConstant.Role.Admin"/>).</summary>
        /// <param name="authId"><c>sub</c> claim of the caller.</param>
        /// <param name="userName"><c>preferred_username</c> claim; <c>user-&lt;authId&gt;</c> by default.</param>
        /// <param name="email"><c>email</c> claim; <c>&lt;authId&gt;@example.test</c> by default.</param>
        public HttpClient AsAdmin(string authId = "admin-1", string? userName = null, string? email = null)
        {
            return client.As(TestUser.Create(authId, [ApiConstant.Role.Admin], userName, email));
        }

        /// <summary>Calls the API as a user of the dataset (<see cref="TestUsers"/>), without role.</summary>
        /// <param name="user">User of the dataset whose identity is sent.</param>
        public HttpClient AsUser(TestUserData user)
        {
            return client.AsUser(user.AuthId, user.UserName, user.Email);
        }

        /// <summary>Calls the API as a user of the dataset (<see cref="TestUsers"/>), with the admin role.</summary>
        /// <param name="user">User of the dataset whose identity is sent.</param>
        public HttpClient AsAdmin(TestUserData user)
        {
            return client.AsAdmin(user.AuthId, user.UserName, user.Email);
        }

        /// <summary>Calls the API as <paramref name="user"/>.</summary>
        /// <param name="user">Identity to send; replaces the current one.</param>
        public HttpClient As(TestUser user)
        {
            client.AsAnonymous();

            client.DefaultRequestHeaders.Add(TestAuthHandler.AuthIdHeader, user.AuthId);
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', user.Roles));
            AddIfNotNull(TestAuthHandler.UserNameHeader, user.UserName);
            AddIfNotNull(TestAuthHandler.DisplayNameHeader, user.DisplayName ?? user.UserName);
            AddIfNotNull(TestAuthHandler.EmailHeader, user.Email);

            return client;

            void AddIfNotNull(string header, string? value)
            {
                if (value is not null)
                    client.DefaultRequestHeaders.Add(header, value);
            }
        }

        /// <summary>
        /// Gets a token from <c>GET /misc/anti-forgery-token</c> and sends it back like a SPA does: the
        /// <c>XSRF-TOKEN</c> cookie (kept by the client) copied into the <c>X-XSRF-TOKEN</c> header.
        /// </summary>
        /// <param name="ct">Cancellation token of the token request.</param>
        public async Task<HttpClient> WithAntiforgeryTokenAsync(CancellationToken ct = default)
        {
            using var response = await client.GetAsync("/misc/anti-forgery-token", ct);
            response.EnsureSuccessStatusCode();

            var token = AntiforgeryCookie(response)
                        ?? throw new InvalidOperationException($"No {AntiforgeryCookieName} cookie issued.");

            client.DefaultRequestHeaders.Remove(AntiforgeryHeaderName);
            client.DefaultRequestHeaders.Add(AntiforgeryHeaderName, token);

            return client;
        }
    }

    /// <summary>Value of the antiforgery cookie set by <paramref name="response"/>, if any.</summary>
    /// <param name="response">Response that may set the cookie.</param>
    public static string? AntiforgeryCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        return cookies
            .Select(cookie => cookie.Split(';')[0].Split('=', 2))
            .Where(pair => pair is [AntiforgeryCookieName, _])
            .Select(pair => pair[1])
            .FirstOrDefault();
    }
}

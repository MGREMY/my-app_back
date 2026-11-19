using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MyApp.Host.Api.Tests.TestInfrastructure;

/// <summary>
/// Authentication scheme replacing JWT bearer in the test host: it authenticates the identity described by the
/// headers that <see cref="ApiClientAuthExtensions"/> sets, with the claims read by <c>ClaimsPrincipalExtension</c>.
/// No header: anonymous (challenge returns 401, forbid returns 403, like JWT bearer).
/// </summary>
/// <remarks>
/// The claims are the ones of a Keycloak access token after the JWT bearer inbound mapping: <c>sub</c> becomes
/// <see cref="ClaimTypes.NameIdentifier"/>, <c>email</c> becomes <see cref="ClaimTypes.Email"/>, while
/// <c>preferred_username</c> (the username) and <c>name</c> (the full name) are kept as is. A missing header omits
/// its claim, like a token without it.
/// </remarks>
/// <remarks>
/// BFF switch: only this handler, <see cref="ApiClientAuthExtensions"/> and <see cref="ApiFactory"/> should change.
/// </remarks>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    private const string PreferredUserNameClaim = "preferred_username";
    private const string NameClaim = "name";

    internal const string AuthIdHeader = "X-Test-Auth-Id";
    internal const string UserNameHeader = "X-Test-User-Name";
    internal const string DisplayNameHeader = "X-Test-Display-Name";
    internal const string EmailHeader = "X-Test-Email";
    internal const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;

        if (!headers.TryGetValue(AuthIdHeader, out var authId))
            return Task.FromResult(AuthenticateResult.NoResult());

        List<Claim> claims = [new(ClaimTypes.NameIdentifier, authId.ToString())];
        AddClaimFromHeader(UserNameHeader, PreferredUserNameClaim);
        AddClaimFromHeader(DisplayNameHeader, NameClaim);
        AddClaimFromHeader(EmailHeader, ClaimTypes.Email);
        claims.AddRange(headers[RolesHeader]
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, NameClaim, ClaimTypes.Role);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));

        void AddClaimFromHeader(string header, string claimType)
        {
            if (headers.TryGetValue(header, out var value))
                claims.Add(new Claim(claimType, value.ToString()));
        }
    }
}

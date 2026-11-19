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
/// BFF switch: only this handler, <see cref="ApiClientAuthExtensions"/> and <see cref="ApiFactory"/> should change.
/// </remarks>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    internal const string AuthIdHeader = "X-Test-Auth-Id";
    internal const string UserNameHeader = "X-Test-User-Name";
    internal const string EmailHeader = "X-Test-Email";
    internal const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headers = Request.Headers;

        if (!headers.TryGetValue(AuthIdHeader, out var authId))
            return Task.FromResult(AuthenticateResult.NoResult());

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, authId.ToString()),
            new(ClaimTypes.Name, headers[UserNameHeader].ToString()),
            new(ClaimTypes.Email, headers[EmailHeader].ToString())
        ];
        claims.AddRange(headers[RolesHeader]
            .ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}

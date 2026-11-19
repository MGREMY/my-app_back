using System.Security.Claims;
using MyApp.Host.Api.Extension;

namespace MyApp.Host.Api.Tests.Extension;

public sealed partial class ClaimsPrincipalExtensionTests
{
    [Trait(TestCategory.Name, TestCategory.Unit)]
    public sealed class Unit
    {
        [Fact]
        public void CurrentId_NameIdentifierClaim_ReturnsIt()
        {
            Principal(new Claim(ClaimTypes.NameIdentifier, "auth-1")).CurrentId.ShouldBe("auth-1");
        }

        // The user name is the Keycloak username (preferred_username, kept as is by the JWT bearer inbound mapping),
        // not the full name ("name").

        [Fact(Skip = "Bug: CurrentUserName only reads ClaimTypes.Name or \"name\" and throws InvalidOperationException "
                     + "when the token only carries preferred_username.")]
        public void CurrentUserName_PreferredUsernameClaim_ReturnsIt()
        {
            Principal(new Claim("preferred_username", "jdoe")).CurrentUserName.ShouldBe("jdoe");
        }

        [Fact(Skip = "Bug: CurrentUserName returns the full name (\"name\" claim) instead of the Keycloak username "
                     + "(preferred_username).")]
        public void CurrentUserName_PreferredUsernameAndNameClaims_ReturnsPreferredUsername()
        {
            Principal(new Claim("name", "John Doe"), new Claim("preferred_username", "jdoe")).CurrentUserName.ShouldBe("jdoe");
        }

        [Fact]
        public void CurrentEmail_EmailClaim_ReturnsIt()
        {
            Principal(new Claim(ClaimTypes.Email, "john@example.test")).CurrentEmail.ShouldBe("john@example.test");
        }

        [Fact]
        public void CurrentId_OnlySubClaim_Throws()
        {
            // The raw "sub" claim is not read: the API relies on the inbound mapping to NameIdentifier.
            Should.Throw<InvalidOperationException>(() => Principal(new Claim("sub", "auth-1")).CurrentId);
        }

        [Fact]
        public void CurrentEmail_NoEmailClaim_Throws()
        {
            Should.Throw<InvalidOperationException>(() => Principal(new Claim("preferred_username", "john")).CurrentEmail);
        }

        [Fact]
        public void CurrentUserName_NoClaim_Throws()
        {
            Should.Throw<InvalidOperationException>(() => new ClaimsPrincipal(new ClaimsIdentity()).CurrentUserName);
        }

        private static ClaimsPrincipal Principal(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        }
    }
}

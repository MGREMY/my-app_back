using System.Security.Claims;
using MyApp.Host.Api.Extension;

namespace MyApp.Host.Api.Tests.Extension;

public sealed partial class ClaimsPrincipalExtensionTests
{
    [UnitTest]
    public sealed class Unit
    {
        [Fact]
        public void CurrentId_NameIdentifierClaim_ReturnsIt()
        {
            Principal(new Claim(ClaimTypes.NameIdentifier, "auth-1")).CurrentId.ShouldBe("auth-1");
        }

        [Fact]
        public void CurrentUserName_NameClaimType_ReturnsIt()
        {
            Principal(new Claim(ClaimTypes.Name, "john")).CurrentUserName.ShouldBe("john");
        }

        [Fact]
        public void CurrentUserName_ShortNameClaim_ReturnsIt()
        {
            // "name" is the OIDC claim, kept as is by the JWT bearer inbound mapping.
            Principal(new Claim("name", "john")).CurrentUserName.ShouldBe("john");
        }

        [Fact]
        public void CurrentUserName_BothNameClaims_ReturnsTheFirstOne()
        {
            Principal(new Claim("name", "first"), new Claim(ClaimTypes.Name, "second")).CurrentUserName.ShouldBe("first");
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
            Should.Throw<InvalidOperationException>(() => Principal(new Claim(ClaimTypes.Name, "john")).CurrentEmail);
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

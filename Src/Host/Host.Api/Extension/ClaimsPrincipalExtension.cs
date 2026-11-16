using System.Security.Claims;

namespace Host.Api.Extension;

public static class ClaimsPrincipalExtension
{
    extension(ClaimsPrincipal principal)
    {
        public string CurrentId => principal.Claims.First(c => c.Type is ClaimTypes.NameIdentifier).Value;
        public string CurrentUserName => principal.Claims.First(claim => claim.Type is ClaimTypes.Name or "name").Value;
        public string CurrentEmail => principal.Claims.First(claim => claim.Type is ClaimTypes.Email).Value;
    }
}
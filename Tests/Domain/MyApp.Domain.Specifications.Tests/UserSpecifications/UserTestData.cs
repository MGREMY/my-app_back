using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Specifications.Tests.UserSpecifications;

internal static class UserTestData
{
    public static User Create(Guid id, string authId)
    {
        return new User { Id = id, AuthId = authId, UserName = $"user-{authId}", Email = $"{authId}@example.test" };
    }
}

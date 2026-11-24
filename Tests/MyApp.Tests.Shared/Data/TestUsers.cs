namespace MyApp.Tests.Shared.Data;

/// <summary>A user of <see cref="TestUsers"/>: plain data, turned into a new entity each time it is seeded.</summary>
/// <param name="Id">Fixed, so a test can find the row it changed.</param>
/// <param name="AuthId">Identity provider id (<c>sub</c> claim): <c>auth-&lt;name&gt;</c>.</param>
/// <param name="UserName">Username (<c>preferred_username</c> claim): the name of the user.</param>
/// <param name="Email"><c>&lt;name&gt;@example.test</c>.</param>
/// <param name="IsDeleted">Whether the user is seeded soft-deleted.</param>
public sealed record TestUserData(Guid Id, string AuthId, string UserName, string Email, bool IsDeleted);

/// <summary>
/// The users dataset of the integration and functional tests, seeded whole by the base classes
/// (<c>SeedUsersAsync()</c>) when a test needs users. Tests take the users they act on from here and derive counts from
/// it (<c>TestUsers.Active.Count</c>), so adding a user does not break unrelated tests.
/// </summary>
/// <remarks>
/// Names are chosen so that their order is obvious (alice &lt; alina &lt; bob &lt; carl): tests write expected orders and
/// pages by hand instead of sorting this list, which would re-implement what they check. Only plain data lives here,
/// never entities: a shared entity would be tracked and mutated across tests.
/// </remarks>
public static class TestUsers
{
    public static readonly TestUserData Alice = Create("alice", "019a0000-0000-7000-8000-000000000001", isDeleted: false);
    public static readonly TestUserData Alina = Create("alina", "019a0000-0000-7000-8000-000000000002", isDeleted: false);
    public static readonly TestUserData Bob = Create("bob", "019a0000-0000-7000-8000-000000000003", isDeleted: false);
    public static readonly TestUserData Carl = Create("carl", "019a0000-0000-7000-8000-000000000004", isDeleted: false);
    public static readonly TestUserData Dave = Create("dave", "019a0000-0000-7000-8000-000000000005", isDeleted: true);
    public static readonly TestUserData Erin = Create("erin", "019a0000-0000-7000-8000-000000000006", isDeleted: true);

    /// <summary>Every user, soft-deleted ones included.</summary>
    public static readonly IReadOnlyList<TestUserData> All = [Alice, Alina, Bob, Carl, Dave, Erin];

    /// <summary>Users visible through the services (not soft-deleted).</summary>
    public static readonly IReadOnlyList<TestUserData> Active = [.. All.Where(x => !x.IsDeleted)];

    /// <summary>Soft-deleted users.</summary>
    public static readonly IReadOnlyList<TestUserData> Deleted = [.. All.Where(x => x.IsDeleted)];

    private static TestUserData Create(string name, string id, bool isDeleted)
    {
        return new TestUserData(Guid.Parse(id), $"auth-{name}", name, $"{name}@example.test", isDeleted);
    }
}

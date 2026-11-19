namespace MyApp.Services.Generators.Tests.TestModels;

/// <summary>
/// Dataset shared by every integration test (read-only). Five authors, ranked 1 to 5 in name order:
/// <list type="bullet">
/// <item>every comparable scalar is <c>f(rank)</c> (Age = 10 x rank...), its nullable twin is the same value, or null for Bob and Eve;</item>
/// <item>Bob has no country, no post, no tag and no holiday (empty collections);</item>
/// <item>mentors form a chain: Bob -> Alice, Carol -> Bob, Dave -> Carol, Eve -> Dave.</item>
/// </list>
/// </summary>
public static class PaginationTestData
{
    public const string Alice = nameof(Alice);
    public const string Bob = nameof(Bob);
    public const string Carol = nameof(Carol);
    public const string Dave = nameof(Dave);
    public const string Eve = nameof(Eve);

    public static readonly string[] All = [Alice, Bob, Carol, Dave, Eve];

    /// <summary>Rank of each author: the value of the comparable scalars is derived from it.</summary>
    public static int RankOf(string name)
    {
        return Array.IndexOf(All, name) + 1;
    }

    public static Guid ExternalId(int rank) => new($"0000000{rank}-0000-0000-0000-000000000000");

    public static async Task SeedAsync(PaginationTestDbContext db, CancellationToken ct)
    {
        var europe = new Continent { Name = "Europe" };
        var asia = new Continent { Name = "Asia" };
        var america = new Continent { Name = "America" };
        var france = new Country { Name = "France", ContinentId = Guid.Empty, Continent = europe };
        var japan = new Country { Name = "Japan", ContinentId = Guid.Empty, Continent = asia };
        var canada = new Country { Name = "Canada", ContinentId = Guid.Empty, Continent = america };

        var alice = NewAuthor(Alice, isActive: true, isVerified: true, AuthorStatus.Draft, AuthorStatus.Draft, france);
        alice.Nickname = "Ali";
        alice.@class = "admin";
        alice.Tags = ["ef", "dotnet"];
        alice.Holidays = [new DateOnly(2024, 12, 25)];
        alice.Avatar = [1, 2, 3];
        alice.Posts = [NewPost("Intro to EF", 10, "ef", "intro"), NewPost("Advanced LINQ", 50, "linq")];

        var bob = NewAuthor(Bob, isActive: false, isVerified: null, AuthorStatus.Published, null, null);
        bob.Mentor = alice;

        var carol = NewAuthor(Carol, isActive: true, isVerified: false, AuthorStatus.Archived, AuthorStatus.Archived, japan);
        carol.Nickname = "";
        carol.Tags = ["cooking"];
        carol.Holidays = [new DateOnly(2024, 7, 14), new DateOnly(2024, 12, 25)];
        carol.Posts = [NewPost("Cooking", 5)];
        carol.Mentor = bob;

        var dave = NewAuthor(Dave, isActive: false, isVerified: true, AuthorStatus.Published, AuthorStatus.Published, france);
        dave.Nickname = "null";
        dave.Tags = ["EF", "travel"];
        dave.Holidays = [new DateOnly(2024, 1, 1)];
        dave.Posts = [NewPost("EF tips", null, "ef"), NewPost("Travel", 20)];
        dave.Mentor = carol;

        var eve = NewAuthor(Eve, isActive: true, isVerified: null, AuthorStatus.Draft, null, canada);
        eve.Nickname = "ALIEN";
        eve.Tags = ["music", "ef"];
        eve.Posts = [NewPost("Music", 30, "music", "ef")];
        eve.Mentor = dave;

        db.Authors.AddRange(alice, bob, carol, dave, eve);
        await db.SaveChangesAsync(ct);
    }

    private static Author NewAuthor(
        string name, bool isActive, bool? isVerified, AuthorStatus status, AuthorStatus? previousStatus, Country? country)
    {
        var rank = RankOf(name);
        var hasNullables = name is not (Bob or Eve);

        var createdAt = new DateTime(2024, 1, rank, 10, 0, 0, DateTimeKind.Utc);
        var joinedAt = new DateTimeOffset(2024, rank, 1, 8, 0, 0, TimeSpan.Zero);
        var birthDate = new DateOnly(1990, 1, rank);

        return new Author
        {
            Name = name,
            IsActive = isActive,
            IsVerified = isVerified,
            Status = status,
            PreviousStatus = previousStatus,
            ExternalId = ExternalId(rank),
            ExternalRef = hasNullables ? ExternalId(rank) : null,
            Age = 10 * rank,
            Rank = hasNullables ? 10 * rank : null,
            Followers = 1_000_000_000_000L * rank,
            Views = hasNullables ? 1_000_000_000_000L * rank : null,
            Balance = rank + 0.25m,
            Bonus = hasNullables ? rank + 0.25m : null,
            Score = rank + 0.5,
            Rating = hasNullables ? rank + 0.5 : null,
            CreatedAt = createdAt,
            UpdatedAt = hasNullables ? createdAt : null,
            JoinedAt = joinedAt,
            LastSeenAt = hasNullables ? joinedAt : null,
            BirthDate = birthDate,
            DeathDate = hasNullables ? birthDate : null,
            Timeout = TimeSpan.FromHours(rank),
            Country = country
        };
    }

    private static Post NewPost(string title, int? likes, params string[] tags)
    {
        return new Post
        {
            Title = title,
            Likes = likes,
            AuthorId = Guid.Empty,
            Tags = tags.Select(x => new Tag { Label = x, PostId = Guid.Empty }).ToList()
        };
    }
}

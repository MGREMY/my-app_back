namespace MyApp.Services.Generators.Tests.TestModels;

// Test-only entity model, declared in this assembly: the generator only follows navigations into classes of the
// assembly of the root entity, so every navigated entity lives here, next to the paginated root (Author).

public enum AuthorStatus
{
    Draft,
    Published,
    Archived
}

/// <summary>Base class: its <see cref="Id" /> is an inherited member, filterable and sortable.</summary>
public abstract class TestEntity
{
    public Guid Id { get; set; }
}

/// <summary>Implemented explicitly by <see cref="Author" />: explicit implementations are not filterable.</summary>
public interface IAudited
{
    string? AuditNote { get; }
}

/// <summary>Paginated root: one scalar of every supported type (and its nullable version) plus every kind of navigation.</summary>
public class Author : TestEntity, IAudited
{
    public required string Name { get; set; }
    public string? Nickname { get; set; }

    // A property named after a C# keyword keeps its "@" in paths: "@class"
    public string? @class { get; set; }

    public required bool IsActive { get; set; }
    public bool? IsVerified { get; set; }
    public required AuthorStatus Status { get; set; }
    public AuthorStatus? PreviousStatus { get; set; }
    public required Guid ExternalId { get; set; }
    public Guid? ExternalRef { get; set; }

    public required int Age { get; set; }
    public int? Rank { get; set; }
    public required long Followers { get; set; }
    public long? Views { get; set; }
    public required decimal Balance { get; set; }
    public decimal? Bonus { get; set; }
    public required double Score { get; set; }
    public double? Rating { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public required DateTimeOffset JoinedAt { get; set; }
    public DateTimeOffset? LastSeenAt { get; set; }
    public required DateOnly BirthDate { get; set; }
    public DateOnly? DeathDate { get; set; }

    // Collections of values
    public List<string> Tags { get; set; } = [];
    public DateOnly[] Holidays { get; set; } = [];

    // Ignored by the generator: binary column and unsupported type
    public byte[]? Avatar { get; set; }
    public TimeSpan Timeout { get; set; }

    public Guid? CountryId { get; set; }
    public virtual Country? Country { get; set; } = null;
    public Guid? MentorId { get; set; }
    public virtual Author? Mentor { get; set; } = null;
    public virtual ICollection<Post> Posts { get; set; } = [];

    string? IAudited.AuditNote => "audited";
}

public class Country : TestEntity
{
    public required string Name { get; set; }
    public required Guid ContinentId { get; set; }
    public virtual Continent Continent { get; set; } = null!;
}

public class Continent : TestEntity
{
    public required string Name { get; set; }
}

public class Post : TestEntity
{
    public required string Title { get; set; }
    public int? Likes { get; set; }
    public required Guid AuthorId { get; set; }
    public virtual Author Author { get; set; } = null!;
    public virtual ICollection<Tag> Tags { get; set; } = [];
}

public class Tag : TestEntity
{
    public required string Label { get; set; }
    public required Guid PostId { get; set; }
    public virtual Post Post { get; set; } = null!;
}

/// <summary>Marks <see cref="Author" /> as a paginated root: generates <c>AuthorPaginationExtensions</c>.</summary>
[PaginationHandlerFor<Author>]
public sealed class AuthorPaginationHandler;

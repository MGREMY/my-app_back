namespace Domain.Service.CachedDto;

internal sealed class CachedUser
{
    public required Guid Id { get; init; }
    public required string AuthId { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset? UpdatedAtUtc { get; init; }
    public required bool HasBeenModified { get; init; }
    public required bool IsDeleted { get; init; }
    public required DateTimeOffset? DeletedAtUtc { get; init; }
}
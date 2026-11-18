using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.CacheModels;

internal sealed class CacheUser
{
    public required Guid Id { get; init; }
    public required string AuthId { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required DateTimeOffset? UpdatedAtUtc { get; init; }
    public required bool HasBeenModified { get; init; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAtUtc { get; set; }

    public static implicit operator UserServiceResponse(CacheUser user)
    {
        return new UserServiceResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}
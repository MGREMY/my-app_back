namespace MyApp.Services.Abstractions.Responses;

public sealed class UserServiceResponse
{
    public required Guid Id { get; init; }
    public required DateTimeOffset CreatedAtUtc { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }
}
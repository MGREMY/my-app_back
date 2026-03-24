namespace Domain.Service.Contract.Dto;

public sealed class UserResponse
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
}

public sealed class MinimalUserResponse
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
}
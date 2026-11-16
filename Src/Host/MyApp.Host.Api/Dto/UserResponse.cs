namespace MyApp.Host.Api.Dto;

public sealed record UserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email, bool IsDeleted)
{
    public UserResponse(MyApp.Service.Abstractions.Dto.UserResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email, value.IsDeleted)

    {
    }
}
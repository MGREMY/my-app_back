namespace Host.Api.Dto;

public record UserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email, bool IsDeleted)
{
    public UserResponse(ServiceDto.UserResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email, value.IsDeleted)

    {
    }
}

public record MinimalUserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email, bool IsDeleted)
{
    public MinimalUserResponse(ServiceDto.MinimalUserResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email, value.IsDeleted)

    {
    }
}
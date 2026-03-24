namespace Host.Api.Dto;

public record UserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email)
{
    public UserResponse(ServiceDto.UserResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email)

    {
    }
}

public record MinimalUserResponse(Guid Id, DateTimeOffset CreatedAtUtc, string UserName, string Email)
{
    public MinimalUserResponse(ServiceDto.MinimalUserResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email)

    {
    }
}
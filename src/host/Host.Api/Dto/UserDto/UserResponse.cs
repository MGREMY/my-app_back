using Domain.Service.Contract.Dto.UserDto;

namespace Host.Api.Dto.UserDto;

public record UserResponse(Guid Id, DateTime CreatedAtUtc, string UserName, string Email)
{
    public UserResponse(UserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email)

    {
    }
}
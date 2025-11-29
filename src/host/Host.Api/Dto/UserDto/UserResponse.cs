using Domain.Service.Contract.Dto.UserDto;

namespace Host.Api.Dto.UserDto;

public record UserResponse(Guid Id, DateTime CreatedAtUtc)
{
    public UserResponse(UserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc)

    {
    }
}
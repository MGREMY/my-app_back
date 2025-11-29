using Domain.Service.Contract.Dto.UserDto;

namespace Host.Api.Dto.UserDto;

public record MinimalUserResponse(Guid Id, DateTime CreatedAtUtc)
{
    public MinimalUserResponse(MinimalUserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc)

    {
    }
}
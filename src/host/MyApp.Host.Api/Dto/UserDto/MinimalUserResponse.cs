using MyApp.Domain.Service.Contract.Dto.UserDto;

namespace MyApp.Host.Api.Dto.UserDto;

public record MinimalUserResponse(string Id, DateTime CreatedAtUtc)
{
    public MinimalUserResponse(MinimalUserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc)

    {
    }
}
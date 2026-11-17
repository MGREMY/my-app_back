using MyApp.Services.Abstractions.Responses;

namespace MyApp.Host.Api.Responses;

public sealed record UserApiResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    string UserName,
    string Email)
{
    public UserApiResponse(UserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email)

    {
    }
}
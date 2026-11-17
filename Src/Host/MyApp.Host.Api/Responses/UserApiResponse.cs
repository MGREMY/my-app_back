namespace MyApp.Host.Api.Responses;

public sealed record UserApiResponse(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    string UserName,
    string Email,
    bool IsDeleted)
{
    public UserApiResponse(Services.Abstractions.Responses.UserServiceResponse value)
        : this(value.Id, value.CreatedAtUtc, value.UserName, value.Email, value.IsDeleted)

    {
    }
}
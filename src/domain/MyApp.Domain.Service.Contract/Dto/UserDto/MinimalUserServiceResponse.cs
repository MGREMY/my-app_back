namespace MyApp.Domain.Service.Contract.Dto.UserDto;

public class MinimalUserServiceResponse
{
    public required string Id { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}
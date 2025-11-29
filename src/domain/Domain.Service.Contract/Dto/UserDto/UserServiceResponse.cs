namespace Domain.Service.Contract.Dto.UserDto;

public class UserServiceResponse
{
    public required Guid Id { get; set; }
    public required DateTime CreatedAtUtc { get; set; }
}
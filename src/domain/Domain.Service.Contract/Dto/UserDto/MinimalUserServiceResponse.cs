namespace Domain.Service.Contract.Dto.UserDto;

public class MinimalUserServiceResponse
{
    public required Guid Id { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
}
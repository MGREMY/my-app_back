namespace Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;

public record AuthSyncUserServiceRequest(string AuthId, string UserName, string Email) : IByUserServiceRequest;
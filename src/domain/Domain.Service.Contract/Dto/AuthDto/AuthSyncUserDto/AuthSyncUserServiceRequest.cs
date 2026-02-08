namespace Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;

public sealed record AuthSyncUserServiceRequest(string AuthId, string UserName, string Email) : IByUserServiceRequest;
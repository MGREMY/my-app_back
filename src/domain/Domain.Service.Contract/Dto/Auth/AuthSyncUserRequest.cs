namespace Domain.Service.Contract.Dto.Auth;

public sealed record AuthSyncUserRequest(string AuthId, string UserName, string Email);
using Core.Service;
using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;

namespace Domain.Service.Contract.Service.AuthService;

public interface IAuthSyncUserService
    : IServiceAsync<AuthSyncUserServiceRequest>;
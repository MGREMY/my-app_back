using MyApp.Core.Service;
using MyApp.Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;

namespace MyApp.Domain.Service.Contract.Service.AuthService;

public interface IAuthSyncUserService
    : IServiceAsync<AuthSyncUserServiceRequest>;
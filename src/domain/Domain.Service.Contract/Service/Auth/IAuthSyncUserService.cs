using Core.Service;
using Domain.Service.Contract.Dto.Auth;

namespace Domain.Service.Contract.Service.Auth;

public interface IAuthSyncUserService
    : IServiceAsync<AuthSyncUserRequest>;
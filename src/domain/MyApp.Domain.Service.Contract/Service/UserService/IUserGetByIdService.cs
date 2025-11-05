using MyApp.Core.Service;
using MyApp.Domain.Service.Contract.Dto.UserDto;
using MyApp.Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;

namespace MyApp.Domain.Service.Contract.Service.UserService;

public interface IUserGetByIdService
    : IServiceAsync<UserGetByIdServiceRequest, UserServiceResponse>;
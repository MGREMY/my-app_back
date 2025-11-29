using Core.Service;
using Domain.Service.Contract.Dto.UserDto;
using Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;

namespace Domain.Service.Contract.Service.UserService;

public interface IUserGetByIdService
    : IServiceAsync<UserGetByIdServiceRequest, UserServiceResponse>;
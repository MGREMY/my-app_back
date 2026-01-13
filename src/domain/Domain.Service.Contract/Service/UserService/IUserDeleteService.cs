using Core.Service;
using Domain.Service.Contract.Dto.UserDto.UserDeleteDto;

namespace Domain.Service.Contract.Service.UserService;

public interface IUserDeleteService
    : IServiceAsync<UserDeleteServiceRequest>;
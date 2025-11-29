using Core.Service;
using Domain.Service.Contract.Dto.PaginationDto;
using Domain.Service.Contract.Dto.UserDto;

namespace Domain.Service.Contract.Service.UserService;

public interface IUserGetService
    : IServiceAsync<PaginationServiceRequest, PaginationServiceResponse<MinimalUserServiceResponse>>;
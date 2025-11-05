using MyApp.Core.Service;
using MyApp.Domain.Service.Contract.Dto.PaginationDto;
using MyApp.Domain.Service.Contract.Dto.UserDto;

namespace MyApp.Domain.Service.Contract.Service.UserService;

public interface IUserGetService
    : IServiceAsync<PaginationServiceRequest, PaginationServiceResponse<MinimalUserServiceResponse>>;
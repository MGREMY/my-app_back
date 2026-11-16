using MyApp.Service.Abstractions.Dto;

namespace MyApp.Service.Abstractions.Service.User;

public interface IGetUserByIdService
    : IServiceAsync<Guid, UserResponse>;
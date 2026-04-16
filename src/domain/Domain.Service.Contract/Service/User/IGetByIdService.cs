using Core.Service;
using Domain.Service.Contract.Dto;

namespace Domain.Service.Contract.Service.User;

public interface IGetByIdService
    : IServiceAsync<Guid, UserResponse>;
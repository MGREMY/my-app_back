using Core.Service;
using Domain.Service.Contract.Dto;

namespace Domain.Service.Contract.Service.Admin.User;

public interface IGetUserByIdService
    : IServiceAsync<Guid, UserResponse>;
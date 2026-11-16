using Core.Service;

namespace Domain.Service.Contract.Service.User;

public interface IDeleteUserByIdService
    : IServiceAsync<Guid>;
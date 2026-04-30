using Core.Service;

namespace Domain.Service.Contract.Service.Admin.User;

public interface IDeleteUserByIdService
    : IServiceAsync<Guid>;
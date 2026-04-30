using Core.Service;

namespace Domain.Service.Contract.Service.Admin.User;

public interface IDeleteUserService
    : IServiceAsync<Guid>;
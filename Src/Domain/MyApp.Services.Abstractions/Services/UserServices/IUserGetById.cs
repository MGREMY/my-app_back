using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.Abstractions.Services.UserServices;

public interface IUserGetById : IServiceAsync<Guid, UserServiceResponse>;
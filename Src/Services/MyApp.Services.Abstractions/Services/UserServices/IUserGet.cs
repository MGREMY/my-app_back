using MyApp.Services.Abstractions.Requests;
using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.Abstractions.Services.UserServices;

public interface IUserGet : IServiceAsync<IUserGet.Request, PaginationServiceResponse<UserServiceResponse>>
{
    public sealed record Request(PaginationServiceRequest PaginationService);
}
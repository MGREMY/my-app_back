using MyApp.Service.Abstractions.Dto;

namespace MyApp.Service.Abstractions.Service.User;

public interface IGetUserService
    : IServiceAsync<IGetUserService.Request, PaginationResponse<MinimalUserResponse>>
{
    public sealed record Request(PaginationRequest PaginationRequest, AdditionalFlags AdditionalFlags);
}
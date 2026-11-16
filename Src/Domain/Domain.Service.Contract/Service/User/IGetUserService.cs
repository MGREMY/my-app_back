using Core.Service;
using Domain.Service.Contract.Dto;

namespace Domain.Service.Contract.Service.User;

public interface IGetUserService
    : IServiceAsync<IGetUserService.Request, PaginationResponse<MinimalUserResponse>>
{
    public sealed record Request(PaginationRequest PaginationRequest, AdditionalFlags AdditionalFlags);
}
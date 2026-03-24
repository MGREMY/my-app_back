using Core.Service;
using Domain.Model;
using Domain.Service.Contract.Dto;
using Domain.Service.Contract.Service.User;
using Domain.Service.Extension;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Service.User;

[PaginationHandlerFor<Model.Model.User>]
public sealed class UserGetService
    : AbstractServiceAsync<PaginationRequest, PaginationResponse<MinimalUserResponse>>,
        IUserGet
{
    private readonly AppDbContext _db;

    public UserGetService(AppDbContext db)
    {
        _db = db;
    }

    protected override Task<PaginationResponse<MinimalUserResponse>> HandleAsync(
        PaginationRequest query,
        CancellationToken ct = default)
    {
        return _db.Users
            .AsNoTracking()
            .ProcessPaginationRequest(query, out var countAsync)
            .Select(ServiceProjection.UserProjection.ToMinimalUserResponse)
            .ToPagedResponseAsync(query.PageNumber, query.PageSize, countAsync, ct);
    }
}
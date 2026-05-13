using Core.Service;
using Domain.Model;
using Domain.Service.Contract.Dto;
using Domain.Service.Contract.Service.User;
using Domain.Service.Extension;
using Domain.Service.Service.Admin.User;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Service.User;

public sealed class UserGet
    : AbstractServiceAsync<PaginationRequest, PaginationResponse<MinimalUserResponse>>,
        IGetUserService
{
    private readonly AppDbContext _db;

    public UserGet(AppDbContext db)
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
using Core.Service;
using Domain.Model;
using Domain.Service.Contract.Dto;
using Domain.Service.Contract.Service.User;
using Domain.Service.Extension;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Service.User;

[PaginationHandlerFor<Model.Model.User>]
public sealed class UserGet
    : AbstractServiceAsync<IGetUserService.Request, PaginationResponse<MinimalUserResponse>>,
        IGetUserService
{
    private readonly AppDbContext _db;

    public UserGet(AppDbContext db)
    {
        _db = db;
    }

    protected override Task<PaginationResponse<MinimalUserResponse>> HandleAsync(
        IGetUserService.Request query,
        CancellationToken ct = default)
    {
        var dbQuery = _db.Users
            .AsNoTracking()
            .AsQueryable();

        if (query.AdditionalFlags.IncludeDeletedItems)
            dbQuery = dbQuery.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]);

        return dbQuery
            .ProcessPaginationRequest(query.PaginationRequest, out var countAsync)
            .Select(ServiceProjection.UserProjection.ToMinimalUserResponse)
            .ToPagedResponseAsync(query.PaginationRequest.PageNumber, query.PaginationRequest.PageSize, countAsync, ct);
    }
}
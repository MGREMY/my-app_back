using MyApp.Domain.Model;
using MyApp.Service.Abstractions.Dto;
using MyApp.Service.Abstractions.Service.User;
using MyApp.Service.Extension;
using Microsoft.EntityFrameworkCore;
using MyApp.Service.Abstractions;

namespace MyApp.Service.Service.User;

[PaginationHandlerFor<Models.User>]
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
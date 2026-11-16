using MyApp.Domain.Model;
using MyApp.Service.Abstractions.Dto;
using MyApp.Service.Abstractions.Service.User;
using MyApp.Service.Extension;
using Microsoft.EntityFrameworkCore;
using MyApp.Service.Abstractions;

namespace MyApp.Service.Service.User;

[PaginationHandlerFor<MyApp.Domain.Model.Model.User>]
public sealed class UserGet : IGetUserService
{
    private readonly AppDbContext _db;

    public UserGet(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResponse<PaginationResponse<UserResponse>>> ExecuteAsync(
        IGetUserService.Request query,
        CancellationToken ct = default)
    {
        var dbQuery = _db.Users
            .AsNoTracking();

        if (query.AdditionalFlags.IncludeDeletedItems)
            dbQuery.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]);

        return await dbQuery
            .ProcessPaginationRequest(query.PaginationRequest, out var countAsync)
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .ToPagedResponseAsync(query.PaginationRequest.PageNumber, query.PaginationRequest.PageSize, countAsync, ct);
    }
}
using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Model;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Extension;

namespace MyApp.Services.Services.UserServices;

[PaginationHandlerFor<MyApp.Domain.Model.Model.User>]
public sealed class UserGet : IUserGet
{
    private readonly AppDbContext _db;

    public UserGet(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResponse<PaginationServiceResponse<UserServiceResponse>>> ExecuteAsync(
        IUserGet.Request query,
        CancellationToken ct = default)
    {
        var dbQuery = _db.Users
            .AsNoTracking();

        if (query.PaginationService.AdditionalFlagsRequest.IncludeDeletedItems)
            dbQuery.IgnoreQueryFilters([ModelConstant.SoftDeletionFilter]);

        return await dbQuery
            .ProcessPaginationRequest(query.PaginationService, out var countAsync)
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .ToPagedResponseAsync(query.PaginationService.PageNumber, query.PaginationService.PageSize, countAsync, ct);
    }
}
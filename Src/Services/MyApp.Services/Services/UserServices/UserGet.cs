using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Extensions;
using MyApp.Services.ServiceProjections;

namespace MyApp.Services.Services.UserServices;

[PaginationHandlerFor<User>]
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
        return await _db.Users
            .AsNoTracking()
            .ProcessPaginationRequest(query.PaginationService, out var countAsync)
            .Select(UserProjection.ToUserServiceResponse)
            .ToPagedResponseAsync(query.PaginationService.PageNumber, query.PaginationService.PageSize, countAsync, ct);
    }
}
using Core.Service;
using Domain.Model;
using Domain.Model.Model.Interface;
using Domain.Service.Contract;
using Domain.Service.Contract.Service.Admin.User;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.Admin.User;

public sealed class UserByIdDelete
    : AbstractServiceAsync<Guid>,
        IDeleteUserByIdService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public UserByIdDelete(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _localizer = localizer;
        _cacheService = cacheService;
    }

    protected override async Task PreExecuteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == id, ct))
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_not_found), 404);
        }
    }

    protected override async Task HandleAsync(
        Guid id,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .Where(user => user.Id == id)
            .FirstAsync(ct);

        user.SetSoftDeletableData();
        await _db.SaveChangesAsync(ct);

        await _cacheService.SaveAsync($"{ServiceConstant.Auth.SyncCacheKey}:{user.AuthId}", user, ct);
    }
}
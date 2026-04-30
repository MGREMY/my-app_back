using Core.Service;
using Domain.Model;
using Domain.Model.Model.Interface;
using Domain.Service.CachedDto;
using Domain.Service.Contract;
using Domain.Service.Contract.Service.Auth;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.Auth;

public sealed class AuthSyncUser
    : AbstractServiceAsync<ISyncUserService.Request>, ISyncUserService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public AuthSyncUser(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _localizer = localizer;
        _cacheService = cacheService;
    }

    protected override async Task HandleAsync(
        ISyncUserService.Request query,
        CancellationToken ct = default)
    {
        var cacheKey = $"{ServiceConstant.Auth.SyncCacheKey}:{query.AuthId}";

        if (await _cacheService.GetAsync<CachedUser>(cacheKey, ct) is { } cacheUser)
        {
            if (!cacheUser.IsDeleted)
            {
                return;
            }

            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_already_deleted), 403);
        }

        var user = await _db.Users
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .AsNoTracking()
            .Where(user => user.AuthId == query.AuthId)
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            user = new Model.Model.User
                {
                    AuthId = query.AuthId,
                    UserName = query.UserName,
                    Email = query.Email,
                }
                .SetCreatedAtData();

            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }

        await _cacheService.SaveAsync(cacheKey, new CachedUser
        {
            Id = user.Id,
            AuthId = user.AuthId,
            UserName = user.UserName,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc =  user.UpdatedAtUtc,
            HasBeenModified = user.HasBeenModified,
            IsDeleted = user.IsDeleted,
            DeletedAtUtc = user.DeletedAtUtc,
        }, ct);

        if (user.IsDeleted)
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_already_deleted), 403);
        }
    }
}
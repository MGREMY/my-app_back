using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Model;
using MyApp.Domain.Model.Model.Interface;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.CachedDto;
using MyApp.Services.Resource;

namespace MyApp.Services.Services.AuthServices;

public sealed class AuthSyncUser : IAuthSyncUser
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

    public async Task<ServiceResponse> ExecuteAsync(IAuthSyncUser.Request query, CancellationToken ct = default)
    {
        var cacheKey = $"{ServiceConstant.Auth.SyncCacheKey}:{query.AuthId}";

        if (await _cacheService.GetAsync<CachedUser>(cacheKey, ct) is { } cacheUser)
        {
            if (!cacheUser.IsDeleted)
            {
                return new ServiceSuccessResponse();
            }

            return new ServiceErrorResponse(403, _localizer.GetString(ServiceConstant.Error.user_already_deleted));
        }

        var user = await _db.Users
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .AsNoTracking()
            .Where(user => user.AuthId == query.AuthId)
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            user = new MyApp.Domain.Model.Model.User
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
            UpdatedAtUtc = user.UpdatedAtUtc,
            HasBeenModified = user.HasBeenModified,
            IsDeleted = user.IsDeleted,
            DeletedAtUtc = user.DeletedAtUtc,
        }, ct);

        if (user.IsDeleted)
        {
            return
                new ServiceErrorResponse(403, _localizer.GetString(ServiceConstant.Error.user_already_deleted));
        }

        return new ServiceSuccessResponse();
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.CachedDto;
using MyApp.Services.Resources;

namespace MyApp.Services.Services.AuthServices;

public sealed class AuthSyncUser : IAuthSyncUser
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cacheService;

    public AuthSyncUser(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _cacheService = cacheService;
    }

    public async Task<ServiceResponse> ExecuteAsync(IAuthSyncUser.Request query, CancellationToken ct = default)
    {
        var cacheKey = $"{ServiceConstant.Auth.SyncCacheKey}:{query.AuthId}";

        if (await _cacheService.GetAsync<CachedUser>(cacheKey, ct) is not null)
        {
            return new ServiceSuccessResponse();
        }

        var user = await _db.Users
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .AsNoTracking()
            .Where(user => user.AuthId == query.AuthId)
            .FirstOrDefaultAsync(ct);

        if (user is null)
        {
            user = new User
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
        }, ct);

        return new ServiceSuccessResponse();
    }
}
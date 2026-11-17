using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.AuthServices;
using MyApp.Services.Resources;
using MyApp.Services.ServiceProjections;

namespace MyApp.Services.Services.AuthServices;

public sealed class AuthSyncUser : IAuthSyncUser
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly HybridCache _cacheService;

    public AuthSyncUser(AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        HybridCache cacheService)
    {
        _db = db;
        _cacheService = cacheService;
        _localizer = localizer;
    }

    public async Task<ServiceResponse> ExecuteAsync(IAuthSyncUser.Request query, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            // Ignore filter because some users can be deleted and should be treated as unauthorized
            .IgnoreQueryFilters([ModelConstant.SoftDeletionFilter])
            .Spec(new UserHasAuthIdSpec(query.AuthId))
            .FirstOrDefaultAsync(ct);

        if (user is { IsDeleted: true })
            return new ServiceErrorResponse(403, _localizer.GetString(ServiceConstant.Error.user_already_deleted));

        if (user is null)
        {
            user = new User
                {
                    AuthId = query.AuthId,
                    UserName = query.UserName,
                    Email = query.Email
                }
                .SetCreatedAtData();

            await _db.Users.AddAsync(user, ct);
            await _db.SaveChangesAsync(ct);
        }

        await _cacheService.SetAsync(
            key: ServiceConstant.Cache.UserKey(user.Id),
            value: UserProjection.ToCacheUserFunc(user),
            tags: [ServiceConstant.Cache.UserTag],
            cancellationToken: ct);

        return new ServiceSuccessResponse();
    }
}
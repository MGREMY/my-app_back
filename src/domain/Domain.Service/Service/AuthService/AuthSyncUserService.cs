using Core.Service;
using Domain.Model;
using Domain.Model.Model;
using Domain.Model.Model.Interface;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using Domain.Service.Contract.Service.AuthService;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.AuthService;

public class AuthSyncUserService
    : AbstractServiceAsync<AuthSyncUserServiceRequest>, IAuthSyncUserService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public AuthSyncUserService(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _localizer = localizer;
        _cacheService = cacheService;
    }

    protected override async Task HandleAsync(
        AuthSyncUserServiceRequest query,
        CancellationToken ct = default)
    {
        var cacheKey = $"{ServiceConstant.Auth.SyncCacheKey}:{query.AuthId}";

        if (await _cacheService.GetAsync<User>(cacheKey, ct) is { } cacheUser)
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
            .FirstOrDefaultAsync(user => user.AuthId == query.AuthId, ct);

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

        await _cacheService.SaveAsync(cacheKey, user, ct);

        if (user.IsDeleted)
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_already_deleted), 403);
        }
    }
}
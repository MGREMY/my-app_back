using Core.Service;
using Domain.Model;
using Domain.Model.Model;
using Domain.Model.Model.Interface;
using Domain.Service.Contract.Dto.AuthDto.AuthSyncUserDto;
using Domain.Service.Contract.Service.AuthService;
using Microsoft.EntityFrameworkCore;
using static Domain.Service.ServiceConstant;

namespace Domain.Service.Service.AuthService;

public class AuthSyncUserService
    : AbstractServiceAsync<AuthSyncUserServiceRequest>, IAuthSyncUserService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cacheService;
    private const string CacheKey = $"{AuthCacheKeyPrefix}:sync";

    public AuthSyncUserService(
        AppDbContext db,
        ICacheService cacheService)
    {
        _db = db;
        _cacheService = cacheService;
    }

    protected override async Task HandleAsync(
        AuthSyncUserServiceRequest query,
        CancellationToken ct = default)
    {
        if (await _cacheService.GetAsync<string[]>(CacheKey, ct) is { } cacheValue)
        {
            if (cacheValue.Any(x => string.Equals(x, query.Id, StringComparison.InvariantCulture)))
            {
                return;
            }

            await _cacheService.DeleteAsync(CacheKey, ct);
        }

        var exists = await _db.Users
            .AsNoTracking()
            .AnyAsync(user => user.AuthId == query.Id, ct);

        if (!exists)
        {
            var newUser = new User
            {
                AuthId = query.Id,
            }
                .SetCreatedAtData();

            await _db.Users.AddAsync(newUser, ct);
            await _db.SaveChangesAsync(ct);
        }

        var ids = await _db.Users
            .Select(user => user.AuthId)
            .ToListAsync(ct);

        await _cacheService.SaveAsync(CacheKey, ids, ct);
    }
}

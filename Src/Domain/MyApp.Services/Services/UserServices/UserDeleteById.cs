using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Infrastructure.Specifications.Extensions;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;

namespace MyApp.Services.Services.UserServices;

public sealed class UserDeleteById : IUserDeleteById
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public UserDeleteById(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _localizer = localizer;
        _cacheService = cacheService;
    }

    public async Task<ServiceResponse> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Spec(new UserHasIdSpec(id))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return new ServiceErrorResponse(404, _localizer.GetString(ServiceConstant.Error.user_not_found));

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        await _cacheService.DeleteAsync($"{ServiceConstant.Auth.SyncCacheKey}:{user.AuthId}", ct);

        return new ServiceSuccessResponse();
    }
}
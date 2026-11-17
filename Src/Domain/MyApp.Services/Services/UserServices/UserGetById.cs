using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.CacheModels;
using MyApp.Services.Resources;
using MyApp.Services.ServiceProjections;

namespace MyApp.Services.Services.UserServices;

public sealed class UserGetById : IUserGetById
{
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserGetById(
        AppDbContext db,
        ICacheService cacheService,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _cacheService = cacheService;
        _localizer = localizer;
    }

    public async Task<ServiceResponse<UserServiceResponse>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var cacheKey = ServiceConstant.Cache.UserKey(id);

        if (await _cacheService.GetAsync<CacheUser>(cacheKey, ct) is { } cachedUser)
            return (UserServiceResponse)cachedUser;

        var user = await _db.Users
            .AsNoTracking()
            .Spec(new UserHasIdSpec(id))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return new ServiceErrorResponse(404, _localizer.GetString(ServiceConstant.Error.user_not_found));

        await _cacheService.SaveAsync(cacheKey, (CacheUser)user, ct);

        return UserProjection.ToUserServiceResponseFunc(user);
    }
}
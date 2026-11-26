using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using MyApp.Domain.Models;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.ServiceProjections;

namespace MyApp.Services.Services.UserServices;

public sealed class UserGetById : IUserGetById
{
    private readonly HybridCache _cacheService;
    private readonly AppDbContext _db;

    public UserGetById(AppDbContext db, HybridCache cacheService)
    {
        _db = db;
        _cacheService = cacheService;
    }

    public async Task<ServiceResponse<UserServiceResponse>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var cachedUser = await _cacheService.GetOrCreateAsync(
            key: ServiceConstant.Cache.UserKey(id),
            factory: async token => await _db.Users
                .Spec(new UserHasIdSpec(id))
                .Select(UserProjection.ToCacheUser)
                .FirstOrDefaultAsync(token),
            tags: [ServiceConstant.Cache.UserTag],
            cancellationToken: ct);

        if (cachedUser is null)
            return ServiceErrorResponse.NotFound(ServiceConstant.Error.user_not_found);

        return (UserServiceResponse)cachedUser;
    }
}
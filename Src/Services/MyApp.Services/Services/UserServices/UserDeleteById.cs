using Microsoft.EntityFrameworkCore;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Caching;

namespace MyApp.Services.Services.UserServices;

public sealed class UserDeleteById : IUserDeleteById
{
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _db;

    public UserDeleteById(AppDbContext db, ICacheService cacheService)
    {
        _db = db;
        _cacheService = cacheService;
    }

    public async Task<ServiceResponse> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .Spec(new UserHasIdSpec(id))
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return ServiceErrorResponse.NotFound(ServiceConstant.Error.user_not_found);

        user.SetSoftDeletableData();

        await _db.SaveChangesAsync(ct);
        await _cacheService.RemoveAsync(ServiceConstant.Cache.UserKey(user.Id), ct);

        return new ServiceSuccessResponse();
    }
}
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Models.Models.Interface;
using MyApp.Domain.Specifications.Extensions;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;

namespace MyApp.Services.Services.UserServices;

public sealed class UserDeleteById : IUserDeleteById
{
    private readonly ICacheService _cacheService;
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

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

        user.SetSoftDeletableData();

        await _db.SaveChangesAsync(ct);
        await _cacheService.DeleteAsync(ServiceConstant.Cache.UserKey(user.Id), ct);

        return new ServiceSuccessResponse();
    }
}
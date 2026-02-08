using Core.Service;
using Domain.Model;
using Domain.Model.Model.Interface;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto.UserDto.UserDeleteDto;
using Domain.Service.Contract.Service.UserService;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.UserService;

public sealed class UserDeleteService
    : AbstractServiceAsync<UserDeleteServiceRequest>,
        IUserDeleteService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public UserDeleteService(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer,
        ICacheService cacheService)
    {
        _db = db;
        _localizer = localizer;
        _cacheService = cacheService;
    }

    protected override async Task PreExecuteAsync(
        UserDeleteServiceRequest query,
        CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == query.Id, ct))
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_not_found), 404);
        }
    }

    protected override async Task HandleAsync(
        UserDeleteServiceRequest query,
        CancellationToken ct = default)
    {
        var user = await _db.Users.FirstAsync(user => user.Id == query.Id, ct);

        user.SetSoftDeletableData();
        await _db.SaveChangesAsync(ct);

        await _cacheService.SaveAsync($"{ServiceConstant.Auth.SyncCacheKey}:{user.AuthId}", user, ct);
    }
}
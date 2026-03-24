using Core.Service;
using Domain.Model;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto;
using Domain.Service.Contract.Service.User;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.User;

public sealed class UserGetByIdService
    : AbstractServiceAsync<Guid, UserResponse>,
        IUserGetById
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserGetByIdService(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _localizer = localizer;
    }

    protected override async Task PreExecuteAsync(
        Guid id,
        CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == id, ct))
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_not_found), 404);
        }
    }

    protected override Task<UserResponse> HandleAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return _db.Users
            .AsNoTracking()
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .AsSplitQuery()
            .FirstAsync(user => user.Id == id, ct);
    }
}
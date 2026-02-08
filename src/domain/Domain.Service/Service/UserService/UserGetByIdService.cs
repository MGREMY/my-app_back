using Core.Service;
using Domain.Model;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto.UserDto;
using Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;
using Domain.Service.Contract.Service.UserService;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Domain.Service.Service.UserService;

public sealed class UserGetByIdService
    : AbstractServiceAsync<UserGetByIdServiceRequest, UserServiceResponse>,
        IUserGetByIdService
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
        UserGetByIdServiceRequest query,
        CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(user => user.Id == query.Id, ct))
        {
            throw new DomainException(_localizer.GetString(ServiceConstant.Error.user_not_found), 404);
        }
    }

    protected override Task<UserServiceResponse> HandleAsync(
        UserGetByIdServiceRequest query,
        CancellationToken ct = default)
    {
        return _db.Users
            .AsNoTracking()
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .AsSplitQuery()
            .FirstAsync(user => user.Id == query.Id, ct);
    }
}
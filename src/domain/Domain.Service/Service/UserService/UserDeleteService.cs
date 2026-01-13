using Core.Service;
using Domain.Model;
using Domain.Model.Model.Interface;
using Domain.Service.Contract;
using Domain.Service.Contract.Dto.UserDto.UserDeleteDto;
using Domain.Service.Contract.Service.UserService;
using Domain.Service.Resource;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using static Domain.Service.ServiceConstant.Error;

namespace Domain.Service.Service.UserService;

public class UserDeleteService
    : AbstractServiceAsync<UserDeleteServiceRequest>,
        IUserDeleteService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserDeleteService(
        AppDbContext db,
        IStringLocalizer<SharedResource> localizer)
    {
        _db = db;
        _localizer = localizer;
    }

    protected override async Task PreExecuteAsync(
        UserDeleteServiceRequest query,
        CancellationToken ct = default)
    {
        var exists = await _db.Users
            .AnyAsync(user => user.Id == query.Id, ct);

        if (!exists)
        {
            throw new DomainException(_localizer.GetString(user_not_found), 404);
        }
    }

    protected override async Task HandleAsync(
        UserDeleteServiceRequest query,
        CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstAsync(user => user.Id == query.Id, ct);

        user.SetSoftDeletableData();

        await _db.SaveChangesAsync(ct);
    }
}
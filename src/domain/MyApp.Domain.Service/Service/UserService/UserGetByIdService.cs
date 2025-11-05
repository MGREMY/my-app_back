using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Core.Service;
using MyApp.Domain.Model;
using MyApp.Domain.Service.Contract.Dto.UserDto;
using MyApp.Domain.Service.Contract.Dto.UserDto.UserGetByIdDto;
using MyApp.Domain.Service.Contract.Service.UserService;
using MyApp.Domain.Service.Resource;
using static MyApp.Domain.Service.ServiceConstant.Error;

namespace MyApp.Domain.Service.Service.UserService;

public class UserGetByIdService
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
        var exists = await _db.Users
            .AnyAsync(user => user.Id == query.Id, ct);

        if (!exists)
        {
            ValidationContext.Instance.ThrowError(_localizer.GetString(user_not_found), 404);
        }
    }

    protected override async Task<UserServiceResponse> HandleAsync(
        UserGetByIdServiceRequest query,
        CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .AsSplitQuery()
            .FirstAsync(ct);
    }
}
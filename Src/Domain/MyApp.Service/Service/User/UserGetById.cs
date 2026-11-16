using System.Net;
using MyApp.Domain.Model;
using MyApp.Service.Abstractions;
using MyApp.Service.Abstractions.Dto;
using MyApp.Service.Abstractions.Service.User;
using MyApp.Infrastructure.Specification;
using MyApp.Infrastructure.Specification.Extensions;
using MyApp.Domain.Specification.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Service.Resource;

namespace MyApp.Service.Service.User;

public sealed class UserGetById
    : AbstractServiceAsync<Guid, UserResponse>,
        IGetUserByIdService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UserGetById(
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
        if (!await _db.Users.Spec(new UserHasIdSpec(id)).AnyAsync(ct))
        {
            throw new DomainException(
                HttpStatusCode.NotFound,
                _localizer.GetString(ServiceConstant.Error.user_not_found));
        }
    }

    protected override Task<UserResponse> HandleAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return _db.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Spec(new UserHasIdSpec(id))
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .FirstAsync(ct);
    }
}
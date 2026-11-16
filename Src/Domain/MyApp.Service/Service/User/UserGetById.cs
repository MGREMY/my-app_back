using MyApp.Domain.Model;
using MyApp.Service.Abstractions;
using MyApp.Service.Abstractions.Dto;
using MyApp.Service.Abstractions.Service.User;
using MyApp.Infrastructure.Specification.Extensions;
using MyApp.Domain.Specification.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Service.Resource;

namespace MyApp.Service.Service.User;

public sealed class UserGetById : IGetUserByIdService
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

    public async Task<ServiceResponse<UserResponse>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Spec(new UserHasIdSpec(id))
            .Select(ServiceProjection.UserProjection.ToUserServiceResponse)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return new ServiceErrorResponse(404, _localizer.GetString(ServiceConstant.Error.user_not_found));

        return user;
    }
}
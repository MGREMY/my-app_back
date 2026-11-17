using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Models;
using MyApp.Domain.Specifications.UserSpecifications;
using MyApp.Infrastructure.Specifications.Extensions;
using MyApp.Services.Abstractions;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.Abstractions.Services.UserServices;
using MyApp.Services.Resources;
using MyApp.Services.ServiceProjections;

namespace MyApp.Services.Services.UserServices;

public sealed class UserGetById : IUserGetById
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

    public async Task<ServiceResponse<UserServiceResponse>> ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .AsSplitQuery()
            .Spec(new UserHasIdSpec(id))
            .Select(UserProjection.ToUserServiceResponse)
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return new ServiceErrorResponse(404, _localizer.GetString(ServiceConstant.Error.user_not_found));

        return user;
    }
}
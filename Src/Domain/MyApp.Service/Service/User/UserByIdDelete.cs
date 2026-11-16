using System.Net;
using MyApp.Domain.Model;
using MyApp.Domain.Model.Model.Interface;
using MyApp.Service.Abstractions;
using MyApp.Service.Abstractions.Service.User;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MyApp.Domain.Specification.User;
using MyApp.Infrastructure.Specification.Extensions;
using MyApp.Service.Abstractions.Dto;
using MyApp.Service.Resource;

namespace MyApp.Service.Service.User;

public sealed class UserByIdDelete : IDeleteUserByIdService
{
    private readonly AppDbContext _db;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly ICacheService _cacheService;

    public UserByIdDelete(
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

        return new ServiceSuccessResponse();
    }
}
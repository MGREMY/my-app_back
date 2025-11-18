using Microsoft.EntityFrameworkCore;
using MyApp.Core.Service;
using MyApp.Domain.Model;
using MyApp.Domain.Model.Model;
using MyApp.Domain.Service.Contract.Dto.PaginationDto;
using MyApp.Domain.Service.Contract.Dto.UserDto;
using MyApp.Domain.Service.Contract.Service.UserService;
using MyApp.Domain.Service.Extension;

namespace MyApp.Domain.Service.Service.UserService;

public class UserGetService
    : AbstractServiceAsync<PaginationServiceRequest, PaginationServiceResponse<MinimalUserServiceResponse>>,
        IUserGetService
{
    private readonly AppDbContext _db;

    public UserGetService(AppDbContext db)
    {
        _db = db;
    }

    protected override async Task<PaginationServiceResponse<MinimalUserServiceResponse>> HandleAsync(
        PaginationServiceRequest query,
        CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .ApplySorting(query.SortServiceRequest)
            .ApplyPagination<User, string>(query.PageNumber, query.PageSize)
            .Select(ServiceProjection.UserProjection.ToMinimalUserResponse)
            .ToPagedResponseAsync(query.PageNumber, query.PageSize, _db.Users.CountAsync, ct);
    }
}
using Core.Service;
using Domain.Model;
using Domain.Model.Model;
using Domain.Service.Contract.Dto.PaginationDto;
using Domain.Service.Contract.Dto.UserDto;
using Domain.Service.Contract.Service.UserService;
using Domain.Service.Extension;
using Microsoft.EntityFrameworkCore;

namespace Domain.Service.Service.UserService;

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
        return await Queryable.Select(_db.Users
                .AsNoTracking()
                .ApplySorting(query.SortServiceRequest)
                .ApplyPagination<User, Guid>(query.PageNumber, query.PageSize), ServiceProjection.UserProjection.ToMinimalUserResponse)
            .ToPagedResponseAsync(query.PageNumber, query.PageSize, _db.Users.CountAsync, ct);
    }
}
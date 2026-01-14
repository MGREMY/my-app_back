using System.Linq.Expressions;
using Domain.Model.Model;
using Domain.Service.Contract.Dto.UserDto;

namespace Domain.Service.ServiceProjection;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, MinimalUserServiceResponse>> ToMinimalUserResponse = user =>
        new MinimalUserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
        };

    internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user =>
        new UserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
        };
}
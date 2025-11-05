using System.Linq.Expressions;
using MyApp.Domain.Model.Model;
using MyApp.Domain.Service.Contract.Dto.UserDto;

namespace MyApp.Domain.Service.ServiceProjection;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, MinimalUserServiceResponse>> ToMinimalUserResponse = user =>
        new MinimalUserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
        };

    internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user =>
        new UserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
        };
}
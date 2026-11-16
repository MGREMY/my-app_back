using System.Linq.Expressions;
using Domain.Model.Model;
using Domain.Service.Contract.Dto;

namespace Domain.Service.ServiceProjection;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, MinimalUserResponse>> ToMinimalUserResponse = user =>
        new MinimalUserResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
            IsDeleted = user.IsDeleted,
        };

    internal static readonly Expression<Func<User, UserResponse>> ToUserServiceResponse = user =>
        new UserResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
            IsDeleted = user.IsDeleted,
        };
}
using System.Linq.Expressions;
using MyApp.Domain.Models.Models;
using MyApp.Services.Abstractions.Responses;
using MyApp.Services.CacheModels;

namespace MyApp.Services.ServiceProjections;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user =>
        new UserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email
        };

    internal static readonly Func<User, UserServiceResponse> ToUserServiceResponseFunc =
        ToUserServiceResponse.Compile();

    internal static readonly Expression<Func<User, CacheUser>> ToCacheUser = user =>
        new CacheUser
        {
            Id = user.Id,
            AuthId = user.AuthId,
            UserName = user.UserName,
            Email = user.Email,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            HasBeenModified = user.HasBeenModified,
            IsDeleted = user.IsDeleted,
            DeletedAtUtc = user.DeletedAtUtc,
        };

    internal static readonly Func<User, CacheUser> ToCacheUserFunc =
        ToCacheUser.Compile();
}
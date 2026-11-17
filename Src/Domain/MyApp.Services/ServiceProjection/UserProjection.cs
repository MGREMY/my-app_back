using System.Linq.Expressions;
using MyApp.Domain.Model.Model;
using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.ServiceProjection;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user =>
        new UserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
            IsDeleted = user.IsDeleted,
        };
}
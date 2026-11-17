using System.Linq.Expressions;
using MyApp.Domain.Models.Models;
using MyApp.Services.Abstractions.Responses;

namespace MyApp.Services.ServiceProjections;

internal static class UserProjection
{
    internal static readonly Expression<Func<User, UserServiceResponse>> ToUserServiceResponse = user =>
        new UserServiceResponse
        {
            Id = user.Id,
            CreatedAtUtc = user.CreatedAtUtc,
            UserName = user.UserName,
            Email = user.Email,
        };
}
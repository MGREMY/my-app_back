using System.Linq.Expressions;
using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Specifications.UserSpecifications;

public sealed class UserHasAuthIdSpec : ISpecification<User>
{
    private readonly string _authId;

    public UserHasAuthIdSpec(string authId)
    {
        _authId = authId;
    }

    public Expression<Func<User, bool>> Criteria => x => x.AuthId == _authId;
}
using System.Linq.Expressions;
using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Specifications.UserSpecifications;

public sealed class UserHasIdSpec : ISpecification<User>
{
    private readonly Guid _id;

    public UserHasIdSpec(Guid id)
    {
        _id = id;
    }

    public Expression<Func<User, bool>> Criteria => x => x.Id == _id;
}
using System.Linq.Expressions;
using MyApp.Domain.Models.Models;

namespace MyApp.Domain.Specifications.UserSpecifications;

public class UserHasIdSpec : ISpecification<User>
{
    private readonly Guid _id;

    public Expression<Func<User, bool>> Criteria => x => x.Id == _id;

    public UserHasIdSpec(Guid id)
    {
        _id = id;
    }
}
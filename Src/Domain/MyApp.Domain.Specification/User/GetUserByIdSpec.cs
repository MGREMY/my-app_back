using System.Linq.Expressions;
using MyApp.Infrastructure.Specification;

namespace MyApp.Domain.Specification.User;

public class UserHasIdSpec : ISpecification<global::MyApp.Domain.Model.Model.User>
{
    private readonly Guid _id;

    public Expression<Func<global::MyApp.Domain.Model.Model.User, bool>> Criteria => x => x.Id == _id;

    public UserHasIdSpec(Guid id)
    {
        _id = id;
    }
}
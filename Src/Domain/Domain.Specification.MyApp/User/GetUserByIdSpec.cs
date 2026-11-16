using System.Linq.Expressions;

namespace Domain.Specification.MyApp.User;

public class UserHasIdSpec : ISpecification<Model.Model.User>
{
    private readonly Guid _id;

    public Expression<Func<Model.Model.User, bool>> Criteria => x => x.Id == _id;

    public UserHasIdSpec(Guid id)
    {
        _id = id;
    }
}
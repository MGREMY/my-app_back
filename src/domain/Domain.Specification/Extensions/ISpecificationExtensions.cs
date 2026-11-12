namespace Domain.Specification.Extensions;

public static class ISpecificationExtensions
{
    extension<T>(ISpecification<T> specification)
    {
        public ISpecification<T> And(ISpecification<T> other)
        {
            return new AndSpecification<T>(specification, other);
        }

        public ISpecification<T> Or(ISpecification<T> other)
        {
            return new OrSpecification<T>(specification, other);
        }

        public ISpecification<T> Not()
        {
            return new NotSpecification<T>(specification);
        }
    }
}
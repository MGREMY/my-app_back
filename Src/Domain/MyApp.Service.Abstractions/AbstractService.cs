namespace MyApp.Service.Abstractions;

public interface IService<in TQuery>
{
    public ServiceResponse Execute(TQuery query);
}

public interface IService<in TQuery, TResult>
{
    public ServiceResponse<TResult> Execute(TQuery query);
}
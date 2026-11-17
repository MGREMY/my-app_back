namespace MyApp.Services.Abstractions;

public interface IService<in TQuery>
{
    public ServiceResponse Execute(TQuery query);
}

public interface IService<in TQuery, TResult>
{
    public ServiceResponse<TResult> Execute(TQuery query);
}

public interface IServiceAsync<in TQuery>
{
    public Task<ServiceResponse> ExecuteAsync(TQuery query, CancellationToken ct = default);
}

public interface IServiceAsync<in TQuery, TResult>
{
    public Task<ServiceResponse<TResult>> ExecuteAsync(TQuery query, CancellationToken ct = default);
}
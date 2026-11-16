namespace MyApp.Service.Abstractions;

public interface IServiceAsync<in TQuery>
{
    public Task<ServiceResponse> ExecuteAsync(TQuery query, CancellationToken ct = default);
}

public interface IServiceAsync<in TQuery, TResult>
{
    public Task<ServiceResponse<TResult>> ExecuteAsync(TQuery query, CancellationToken ct = default);
}
namespace Core.Service;

public interface IServiceAsync<TQuery> : IDisposable, IAsyncDisposable
{
    public Task ExecuteAsync(TQuery query, CancellationToken ct = default);
}

public interface IServiceAsync<TQuery, TResult> : IDisposable, IAsyncDisposable
{
    public Task<TResult> ExecuteAsync(TQuery query, CancellationToken ct = default);
}

public abstract class AbstractServiceAsync<TQuery>
    : IServiceAsync<TQuery>
{
    protected abstract Task HandleAsync(TQuery query, CancellationToken ct = default);

    protected virtual Task PreExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task PostExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task ExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        await PreExecuteAsync(query, ct);
        await HandleAsync(query, ct);
        await PostExecuteAsync(query, ct);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}

public abstract class AbstractServiceAsync<TQuery, TResult>
    : IServiceAsync<TQuery, TResult>
{
    protected abstract Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);

    protected virtual Task PreExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    protected virtual Task PostExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public async Task<TResult> ExecuteAsync(TQuery query, CancellationToken ct = default)
    {
        await PreExecuteAsync(query, ct);
        var result = await HandleAsync(query, ct);
        await PostExecuteAsync(query, ct);

        return result;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public virtual ValueTask DisposeAsync()
    {
        Dispose();
        GC.SuppressFinalize(this);

        return ValueTask.CompletedTask;
    }
}
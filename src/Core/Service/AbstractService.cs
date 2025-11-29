namespace Core.Service;

public interface IService<TQuery> : IDisposable
{
    public void Execute(TQuery query);
}

public interface IService<TQuery, TResult> : IDisposable
{
    public TResult Execute(TQuery query);
}

public abstract class AbstractService<TQuery>
    : IService<TQuery>
{
    protected abstract void Handle(TQuery query);

    protected virtual void PreExecute(TQuery query)
    {
    }

    protected virtual void PostExecute(TQuery query)
    {
    }

    public virtual void Execute(TQuery query)
    {
        PreExecute(query);
        Handle(query);
        PostExecute(query);
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

public abstract class AbstractService<TQuery, TResult>
    : IService<TQuery, TResult>
{
    protected abstract TResult Handle(TQuery query);

    protected virtual void PreExecute(TQuery query)
    {
    }

    protected virtual void PostExecute(TQuery query)
    {
    }

    public virtual TResult Execute(TQuery query)
    {
        PreExecute(query);
        var result = Handle(query);
        PostExecute(query);

        return result;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
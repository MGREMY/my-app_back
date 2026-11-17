namespace MyApp.Services.Abstractions;

public interface ICacheData;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : ICacheData;
    Task SaveAsync<T>(string key, T data, CancellationToken ct = default) where T : ICacheData;
    Task DeleteAsync(string key, CancellationToken ct = default);
}
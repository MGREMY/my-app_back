namespace MyApp.Services.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SaveAsync<T>(string key, T data, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}
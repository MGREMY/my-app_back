namespace MyApp.Services.IO;

public interface IFileStorageService
{
    Task<FileStream> GetAsync(string fileName, CancellationToken ct = default);
    Task<string> CreateAsync(CancellationToken ct = default);
    Task<string> SaveAsync(Stream stream, string? fileName = null, bool append = false, CancellationToken ct = default);
    Task<string> SaveAsync(byte[] bytes, string? fileName = null, bool append = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(string fileName, CancellationToken ct = default);
    Task DeleteAsync(string fileName, CancellationToken ct = default);
}
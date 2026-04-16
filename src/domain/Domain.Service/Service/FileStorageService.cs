using Microsoft.Extensions.Options;

namespace Domain.Service.Service;

public interface IFileStorageService
{
    Task<FileStream> GetAsync(string fileName, CancellationToken ct = default);
    Task<string> CreateAsync(CancellationToken ct = default);
    Task<string> SaveAsync(Stream stream, string? fileName = null, bool append = false, CancellationToken ct = default);
    Task<string> SaveAsync(byte[] bytes, string? fileName = null, bool append = false, CancellationToken ct = default);
    Task<bool> ExistsAsync(string fileName, CancellationToken ct = default);
    Task DeleteAsync(string fileName, CancellationToken ct = default);
}

public class FileStorageService : IFileStorageService
{
    private readonly ServiceOption _options;

    public FileStorageService(IOptions<ServiceOption> options)
    {
        _options = options.Value;

        Directory.CreateDirectory(options.Value.DataPath);
    }

    public Task<FileStream> GetAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var filePath = Path.Combine(_options.DataPath, fileName);

        return Task.FromResult(System.IO.File.OpenRead(filePath));
    }

    public Task<string> CreateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var fileName = Guid.NewGuid().ToString();
        var filePath = Path.Combine(_options.DataPath, fileName);

        using var _ = System.IO.File.Create(filePath);

        return Task.FromResult(filePath);
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string? fileName = null,
        bool append = false,
        CancellationToken ct = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        fileName ??= Guid.NewGuid().ToString();
        var filePath = Path.Combine(_options.DataPath, fileName);

        await using var fileStream = append switch
        {
            true => new FileStream(filePath, FileMode.Append, FileAccess.Write),
            false => new FileStream(filePath, FileMode.Create, FileAccess.Write),
        };

        await stream.CopyToAsync(fileStream, ct);

        return fileName;
    }

    public async Task<string> SaveAsync(
        byte[] bytes,
        string? fileName = null,
        bool append = false,
        CancellationToken ct = default)
    {
        await using var memoryStream = new MemoryStream(bytes);

        return await SaveAsync(memoryStream, fileName, append, ct);
    }

    public Task<bool> ExistsAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var filePath = Path.Combine(_options.DataPath, fileName);

        return Task.FromResult(System.IO.File.Exists(filePath));
    }

    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var filePath = Path.Combine(_options.DataPath, fileName);

        System.IO.File.Delete(filePath);

        return Task.CompletedTask;
    }
}
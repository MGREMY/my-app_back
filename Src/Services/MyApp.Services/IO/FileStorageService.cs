using Microsoft.Extensions.Options;

namespace MyApp.Services.IO;

/// <summary>
///     Stores files under <see cref="ServiceOption.DataPath" />.
///     Every method takes and returns file names relative to that folder.
/// </summary>
internal sealed class FileStorageService : IFileStorageService
{
    private readonly string _rootPath;

    public FileStorageService(IOptions<ServiceOption> options)
    {
        _rootPath = Path.GetFullPath(options.Value.DataPath);

        Directory.CreateDirectory(_rootPath);
    }

    public Task<FileStream> GetAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        return Task.FromResult(File.OpenRead(GetSafePath(fileName)));
    }

    public Task<string> CreateAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var fileName = Guid.NewGuid().ToString();

        using var _ = File.Create(GetSafePath(fileName));

        return Task.FromResult(fileName);
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string? fileName = null,
        bool append = false,
        CancellationToken ct = default)
    {
        stream.Seek(0, SeekOrigin.Begin);

        fileName ??= Guid.NewGuid().ToString();
        var filePath = GetSafePath(fileName);

        await using var fileStream = append switch
        {
            true => new FileStream(filePath, FileMode.Append, FileAccess.Write),
            false => new FileStream(filePath, FileMode.Create, FileAccess.Write)
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

        return Task.FromResult(File.Exists(GetSafePath(fileName)));
    }

    public Task DeleteAsync(string fileName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        File.Delete(GetSafePath(fileName));

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Resolve <paramref name="fileName" /> inside the storage folder.
    /// </summary>
    /// <exception cref="ArgumentException">
    ///     If the resolved path is outside the storage folder (absolute path, <c>..</c> segments...)
    /// </exception>
    private string GetSafePath(string fileName)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        var relativePath = Path.GetRelativePath(_rootPath, fullPath);

        var isOutside = relativePath is "." or ".."
                        || relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        || Path.IsPathRooted(relativePath);

        if (isOutside)
            throw new ArgumentException($@"File name '{fileName}' resolves outside of the storage folder.", fileName);

        return fullPath;
    }
}
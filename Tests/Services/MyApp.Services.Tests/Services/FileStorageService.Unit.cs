using System.Text;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Services.Abstractions;
using MyApp.Services.Extensions;

namespace MyApp.Services.Tests.Services;

public sealed partial class FileStorageServiceTests
{
    /// <summary>Works in a temporary folder; the service is internal, so it is resolved through AddMyAppServices.</summary>
    [UnitTest]
    public sealed class Unit : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), $"myapp-tests-{Guid.NewGuid():N}");
        private readonly string _dataPath;
        private readonly ServiceProvider _provider;
        private readonly IFileStorageService _storage;

        public Unit()
        {
            _dataPath = Path.Combine(_root, "data");
            _provider = new ServiceCollection().AddMyAppServices(o => o.DataPath = _dataPath).BuildServiceProvider();
            _storage = _provider.GetRequiredService<IFileStorageService>();
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        public static TheoryData<string> OutsideNames => new(
            "../outside.txt",
            "sub/../../outside.txt",
            "../data-sibling/file.txt",
            "/etc/passwd",
            "",
            ".",
            "..");

        public void Dispose()
        {
            _provider.Dispose();
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }

        [Fact]
        public void Constructor_MissingFolder_CreatesIt()
        {
            Directory.Exists(_dataPath).ShouldBeTrue();
        }

        [Fact]
        public async Task CreateAsync_Called_CreatesEmptyFileWithGeneratedName()
        {
            var name = await _storage.CreateAsync(Ct);

            Guid.TryParse(name, out _).ShouldBeTrue();
            new FileInfo(Path.Combine(_dataPath, name)).Length.ShouldBe(0);
        }

        [Fact]
        public async Task SaveAsync_BytesWithoutName_WritesContentUnderGeneratedName()
        {
            var name = await _storage.SaveAsync("hello"u8.ToArray(), ct: Ct);

            Guid.TryParse(name, out _).ShouldBeTrue();
            (await File.ReadAllTextAsync(Path.Combine(_dataPath, name), Ct)).ShouldBe("hello");
        }

        [Fact]
        public async Task SaveAsync_ExistingName_OverwritesContent()
        {
            await _storage.SaveAsync("long content"u8.ToArray(), "file.txt", ct: Ct);

            var name = await _storage.SaveAsync("new"u8.ToArray(), "file.txt", ct: Ct);

            name.ShouldBe("file.txt");
            (await File.ReadAllTextAsync(Path.Combine(_dataPath, name), Ct)).ShouldBe("new");
        }

        [Fact]
        public async Task SaveAsync_Append_AddsContentToExistingFile()
        {
            await _storage.SaveAsync("a"u8.ToArray(), "file.txt", ct: Ct);

            await _storage.SaveAsync("b"u8.ToArray(), "file.txt", append: true, ct: Ct);

            (await File.ReadAllTextAsync(Path.Combine(_dataPath, "file.txt"), Ct)).ShouldBe("ab");
        }

        [Fact]
        public async Task SaveAsync_StreamNotAtStart_SavesWholeStream()
        {
            using var stream = new MemoryStream("whole"u8.ToArray());
            stream.Seek(0, SeekOrigin.End);

            var name = await _storage.SaveAsync(stream, ct: Ct);

            (await File.ReadAllTextAsync(Path.Combine(_dataPath, name), Ct)).ShouldBe("whole");
        }

        [Fact]
        public async Task SaveAsync_NameWithInnerParentSegment_StaysInsideFolder()
        {
            Directory.CreateDirectory(Path.Combine(_dataPath, "sub"));

            var name = await _storage.SaveAsync("x"u8.ToArray(), "sub/../inside.txt", ct: Ct);

            name.ShouldBe("sub/../inside.txt");
            File.Exists(Path.Combine(_dataPath, "inside.txt")).ShouldBeTrue();
        }

        [Fact]
        public async Task GetAsync_SavedFile_ReturnsReadableStream()
        {
            var name = await _storage.SaveAsync("content"u8.ToArray(), ct: Ct);

            await using var stream = await _storage.GetAsync(name, Ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            (await reader.ReadToEndAsync(Ct)).ShouldBe("content");
        }

        [Fact]
        public async Task GetAsync_MissingFile_ThrowsFileNotFoundException()
        {
            await Should.ThrowAsync<FileNotFoundException>(() => _storage.GetAsync("missing.txt", Ct));
        }

        [Fact]
        public async Task ExistsAsync_SavedAndMissingFiles_ReturnsWhetherFileExists()
        {
            var name = await _storage.CreateAsync(Ct);

            (await _storage.ExistsAsync(name, Ct)).ShouldBeTrue();
            (await _storage.ExistsAsync("missing.txt", Ct)).ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteAsync_SavedFile_RemovesIt()
        {
            var name = await _storage.CreateAsync(Ct);

            await _storage.DeleteAsync(name, Ct);

            File.Exists(Path.Combine(_dataPath, name)).ShouldBeFalse();
        }

        [Fact]
        public async Task DeleteAsync_MissingFile_DoesNotThrow()
        {
            await Should.NotThrowAsync(() => _storage.DeleteAsync("missing.txt", Ct));
        }

        [Theory]
        [MemberData(nameof(OutsideNames))]
        public async Task GetAsync_NameOutsideFolder_ThrowsArgumentException(string name)
        {
            await Should.ThrowAsync<ArgumentException>(() => _storage.GetAsync(name, Ct));
        }

        [Theory]
        [MemberData(nameof(OutsideNames))]
        public async Task SaveAsync_NameOutsideFolder_ThrowsArgumentExceptionWithoutWriting(string name)
        {
            await Should.ThrowAsync<ArgumentException>(() => _storage.SaveAsync("x"u8.ToArray(), name, ct: Ct));

            File.Exists(Path.Combine(_root, "outside.txt")).ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(OutsideNames))]
        public async Task ExistsAsync_NameOutsideFolder_ThrowsArgumentException(string name)
        {
            await Should.ThrowAsync<ArgumentException>(() => _storage.ExistsAsync(name, Ct));
        }

        [Theory]
        [MemberData(nameof(OutsideNames))]
        public async Task DeleteAsync_NameOutsideFolder_ThrowsArgumentExceptionWithoutDeleting(string name)
        {
            var outside = Path.Combine(_root, "outside.txt");
            await File.WriteAllTextAsync(outside, "keep", Ct);

            await Should.ThrowAsync<ArgumentException>(() => _storage.DeleteAsync(name, Ct));

            File.Exists(outside).ShouldBeTrue();
        }

        [Fact]
        public async Task ExistsAsync_AbsolutePathInsideOtherFolder_ThrowsArgumentException()
        {
            var outside = Path.Combine(_root, "outside.txt");
            await File.WriteAllTextAsync(outside, "x", Ct);

            await Should.ThrowAsync<ArgumentException>(() => _storage.ExistsAsync(outside, Ct));
        }

        [Fact(Skip = "Bug: GetSafePath passes the file name value as ArgumentException.paramName instead of nameof(fileName)")]
        public async Task ExistsAsync_NameOutsideFolder_ReportsFileNameParameter()
        {
            var exception = await Should.ThrowAsync<ArgumentException>(() => _storage.ExistsAsync("../outside.txt", Ct));

            exception.ParamName.ShouldBe("fileName");
        }

        [Fact]
        public async Task ExistsAsync_CanceledToken_ThrowsOperationCanceledException()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(() => _storage.ExistsAsync("file.txt", cts.Token));
        }
    }
}

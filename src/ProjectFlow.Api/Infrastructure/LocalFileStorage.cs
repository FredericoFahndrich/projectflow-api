using Microsoft.Extensions.Options;

namespace ProjectFlow.Api.Infrastructure;

public sealed class AttachmentStorageOptions
{
    public const string SectionName = "AttachmentStorage";
    public string RootPath { get; init; } = "uploads";
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
    public string[] AllowedExtensions { get; init; } = [".pdf", ".png", ".jpg", ".jpeg", ".txt"];
}

public sealed record StoredFile(string StoredName, long SizeBytes);

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(IFormFile file, CancellationToken cancellationToken);
    Stream OpenRead(string storedName);
    void Delete(string storedName);
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly AttachmentStorageOptions _options;
    private readonly string _rootPath;

    public LocalFileStorage(IOptions<AttachmentStorageOptions> options, IWebHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.IsPathRooted(_options.RootPath)
            ? _options.RootPath
            : Path.Combine(environment.ContentRootPath, _options.RootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredFile> SaveAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length <= 0 || file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size must be between 1 and {_options.MaxFileSizeBytes} bytes.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"File extension '{extension}' is not allowed.");
        }

        var storedName = $"{Guid.CreateVersion7():N}{extension}";
        var destination = ResolvePath(storedName);
        await using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
        await file.CopyToAsync(stream, cancellationToken);
        return new StoredFile(storedName, file.Length);
    }

    public Stream OpenRead(string storedName) => File.OpenRead(ResolvePath(storedName));

    public void Delete(string storedName)
    {
        var path = ResolvePath(storedName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private string ResolvePath(string storedName)
    {
        var safeName = Path.GetFileName(storedName);
        if (!string.Equals(storedName, safeName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Invalid stored file name.");
        }

        return Path.Combine(_rootPath, safeName);
    }
}


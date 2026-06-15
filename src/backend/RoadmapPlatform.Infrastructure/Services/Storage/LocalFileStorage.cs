using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.Storage;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Security.Cryptography;

namespace RoadmapPlatform.Infrastructure.Services.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly FileStorageSettings _settings;

    public LocalFileStorage(IOptions<FileStorageSettings> options)
    {
        _settings = options.Value;
    }

    public string ProviderName => "Local";

    public async Task<StoredFileDto> SaveAsync(
        string objectPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(objectPath))
        {
            throw new ArgumentException("Object path is required.", nameof(objectPath));
        }

        var safeObjectPath = NormalizeObjectPath(objectPath);
        var rootFolder = GetRootFolder();
        var fullPath = Path.Combine(rootFolder, safeObjectPath);

        var directory = Path.GetDirectoryName(fullPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using (var output = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true))
        {
            await content.CopyToAsync(output, cancellationToken);
            await output.FlushAsync(cancellationToken);
        }

        var hash = await CalculateSha256Async(fullPath, cancellationToken);
        var length = new FileInfo(fullPath).Length;

        return new StoredFileDto(
            safeObjectPath,
            Url: null,
            length,
            hash);
    }

    public Task<Stream> OpenReadAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        var safeObjectPath = NormalizeObjectPath(objectPath);
        var fullPath = Path.Combine(GetRootFolder(), safeObjectPath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Stored file was not found.", fullPath);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        var safeObjectPath = NormalizeObjectPath(objectPath);
        var fullPath = Path.Combine(GetRootFolder(), safeObjectPath);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string GetRootFolder()
    {
        var configuredFolder = string.IsNullOrWhiteSpace(_settings.LocalFolder)
            ? "storage"
            : _settings.LocalFolder;

        var rootFolder = Path.IsPathRooted(configuredFolder)
            ? configuredFolder
            : Path.Combine(AppContext.BaseDirectory, configuredFolder);

        Directory.CreateDirectory(rootFolder);
        return rootFolder;
    }

    private static string NormalizeObjectPath(string objectPath)
    {
        var normalized = objectPath
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Object path cannot contain parent directory traversal.");
        }

        return normalized;
    }

    private static async Task<string> CalculateSha256Async(
        string fullPath,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete,
            bufferSize: 81920,
            useAsync: true);

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

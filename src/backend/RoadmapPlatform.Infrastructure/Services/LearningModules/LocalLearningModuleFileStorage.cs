using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Configurations;
using System.Security.Cryptography;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LocalLearningModuleFileStorage : ILearningModuleFileStorage
{
    private readonly LearningModuleFileStorageSettings _settings;

    public LocalLearningModuleFileStorage(IOptions<LearningModuleFileStorageSettings> options)
    {
        _settings = options.Value;
    }

    public string ProviderName => "Local";

    public async Task<StoredLearningModuleFileDto> SaveAsync(
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

        await using var output = File.Create(fullPath);
        await content.CopyToAsync(output, cancellationToken);

        output.Position = 0;

        var hash = await CalculateSha256Async(fullPath, cancellationToken);
        var length = new FileInfo(fullPath).Length;

        return new StoredLearningModuleFileDto(
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
            throw new FileNotFoundException("Learning module file was not found.", fullPath);
        }

        Stream stream = File.OpenRead(fullPath);
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
            ? "learning-modules"
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
        await using var stream = File.OpenRead(fullPath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

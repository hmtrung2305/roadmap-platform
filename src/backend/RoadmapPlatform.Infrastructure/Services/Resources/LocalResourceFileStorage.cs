using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.Resources;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.Resources
{
    public sealed class LocalResourceFileStorage(
        IWebHostEnvironment environment,
        IOptions<FileStorageSettings> options) : IResourceFileStorage
    {
        private readonly FileStorageSettings _options = options.Value;

        public string ProviderName => "Local";

        public async Task<StoredResourceFile> SaveAsync(
            string objectPath,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            var relativePath = NormalizeObjectPath(objectPath);
            var physicalPath = ResolvePath(relativePath);
            var directory = Path.GetDirectoryName(physicalPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var output = new FileStream(physicalPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await content.CopyToAsync(output, cancellationToken);

            return new StoredResourceFile(relativePath, "/" + relativePath.Replace("\\", "/"));
        }

        public Task<Stream> OpenReadAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePath(NormalizeObjectPath(objectPath));

            if (!File.Exists(physicalPath))
            {
                throw new FileNotFoundException("Resource file was not found.", objectPath);
            }

            Stream stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return Task.FromResult(stream);
        }

        public Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default)
        {
            var physicalPath = ResolvePath(NormalizeObjectPath(objectPath));

            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            return Task.CompletedTask;
        }

        private string ResolvePath(string objectPath)
        {
            var webRootPath = environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            return Path.GetFullPath(Path.Combine(webRootPath, objectPath));
        }

        private string NormalizeObjectPath(string objectPath)
        {
            var normalized = objectPath.Replace("\\", "/").TrimStart('/');
            var localFolder = (_options.LocalFolder ?? "docs").Trim().Trim('/').Replace("\\", "/");

            return normalized.StartsWith(localFolder + "/", StringComparison.OrdinalIgnoreCase)
                ? normalized
                : $"{localFolder}/{normalized}";
        }
    }
}

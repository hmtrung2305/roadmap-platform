using RoadmapPlatform.Application.DTOs.Storage;

namespace RoadmapPlatform.Application.Interfaces.Storage;

public interface IFileStorage
{
    string ProviderName { get; }

    Task<StoredFileDto> SaveAsync(
        string objectPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string objectPath,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default);
}

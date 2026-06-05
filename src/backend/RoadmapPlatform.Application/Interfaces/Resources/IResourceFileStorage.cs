namespace RoadmapPlatform.Application.Interfaces.Resources
{
    public interface IResourceFileStorage
    {
        string ProviderName { get; }

        Task<StoredResourceFile> SaveAsync(
            string objectPath,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(string objectPath, CancellationToken cancellationToken = default);

        Task DeleteAsync(string objectPath, CancellationToken cancellationToken = default);
    }

    public sealed record StoredResourceFile(string ObjectPath, string Url);
}

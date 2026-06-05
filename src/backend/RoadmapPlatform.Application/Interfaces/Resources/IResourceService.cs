using RoadmapPlatform.Application.DTOs.Resources;

namespace RoadmapPlatform.Application.Interfaces.Resources
{
    public interface IResourceService
    {
        Task<List<ResourceResponseDto>> GetResourcesAsync();

        Task<ResourceResponseDto> UploadResourceAsync(
            string title,
            string skillName,
            string originalFileName,
            Stream fileStream,
            long fileLength,
            string contentType);

        Task<string> GetResourceContentAsync(Guid resourceId);

        Task DeleteResourceAsync(Guid resourceId);
    }
}

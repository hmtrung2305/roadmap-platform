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
            long fileLength);

        Task DeleteResourceAsync(Guid resourceId);
    }
}
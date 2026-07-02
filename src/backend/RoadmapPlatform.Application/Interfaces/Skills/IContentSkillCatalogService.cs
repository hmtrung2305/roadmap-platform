using RoadmapPlatform.Application.DTOs.Skills;

namespace RoadmapPlatform.Application.Interfaces.Skills;

public interface IContentSkillCatalogService
{
    Task<ContentSkillSearchResultDto> SearchSkillsAsync(
        ContentSkillSearchQueryDto query,
        CancellationToken cancellationToken);

    Task<ContentSkillDto> GetSkillAsync(
        Guid skillId,
        CancellationToken cancellationToken);

    Task<ContentSkillDto> CreateSkillAsync(
        CreateContentSkillRequestDto request,
        CancellationToken cancellationToken);

    Task<ContentSkillDto> UpdateSkillAsync(
        Guid skillId,
        UpdateContentSkillRequestDto request,
        CancellationToken cancellationToken);
}

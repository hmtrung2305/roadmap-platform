using RoadmapPlatform.Application.DTOs.Skills;

namespace RoadmapPlatform.Application.Interfaces.Skills;

public interface ISkillLookupService
{
    Task<SkillSearchResultDto> SearchSkillsAsync(
        string? search,
        string? category,
        int? limit,
        int? offset,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetCategoriesAsync(
        CancellationToken cancellationToken);
}

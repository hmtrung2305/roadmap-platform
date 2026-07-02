using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerLearningResourceSearchService(ApplicationDbContext dbContext)
{
    public async Task<IReadOnlyList<ContentLearningResourceSearchResultDto>> SearchLearningResourcesAsync(
        string? search,
        int limit,
        CancellationToken cancellationToken)
    {
        var safeLimit = Math.Clamp(limit <= 0 ? 20 : limit, 1, 50);
        var query = dbContext.Set<LearningResource>()
            .AsNoTracking()
            .AsQueryable();

        var normalizedSearch = ContentManagerRoadmapText.NormalizeOptionalText(search);
        if (normalizedSearch is not null)
        {
            var pattern = ContentManagerRoadmapText.BuildContainsPattern(normalizedSearch);

            query = query.Where(resource =>
                EF.Functions.ILike(resource.Title, pattern, "\\")
                || EF.Functions.ILike(resource.Url, pattern, "\\")
                || (resource.Provider != null && EF.Functions.ILike(resource.Provider, pattern, "\\"))
                || (resource.Description != null && EF.Functions.ILike(resource.Description, pattern, "\\")));
        }

        return await query
            .OrderBy(resource => resource.Title)
            .Take(safeLimit)
            .Select(resource => new ContentLearningResourceSearchResultDto
            {
                ResourceId = resource.LearningResourceId,
                Title = resource.Title,
                Url = resource.Url,
                ResourceType = resource.ResourceType,
                Description = resource.Description,
                Provider = resource.Provider,
                DifficultyLevel = resource.DifficultyLevel,
                LanguageCode = resource.LanguageCode,
                VerificationStatus = resource.VerificationStatus
            })
            .ToListAsync(cancellationToken);
    }
}

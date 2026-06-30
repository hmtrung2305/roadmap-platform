using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapUniqueness
{
    private const string DuplicateTitleMessage = "A roadmap with this title already exists for this career role.";

    public static async Task EnsureTitleAvailableAsync(
        ApplicationDbContext dbContext,
        Guid careerRoleId,
        Guid? excludedRoadmapId,
        string title,
        CancellationToken cancellationToken)
    {
        var normalizedTitle = ContentManagerRoadmapText
            .NormalizeRequiredText(title, "Roadmap title is required.")
            .Trim()
            .ToLowerInvariant();

        var titleExists = await dbContext.Set<Roadmap>()
            .AsNoTracking()
            .AnyAsync(roadmap =>
                roadmap.CareerRoleId == careerRoleId
                && (!excludedRoadmapId.HasValue || roadmap.RoadmapId != excludedRoadmapId.Value)
                && roadmap.Title.Trim().ToLower() == normalizedTitle,
                cancellationToken);

        if (titleExists)
        {
            throw new InvalidOperationException(DuplicateTitleMessage);
        }
    }

    public static async Task<string> CreateUniqueSlugAsync(
        ApplicationDbContext dbContext,
        string title,
        CancellationToken cancellationToken)
    {
        var baseSlug = ContentManagerRoadmapText.Slugify(title);
        if (string.Equals(baseSlug, "node", StringComparison.OrdinalIgnoreCase))
        {
            baseSlug = "roadmap";
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await dbContext.Set<Roadmap>()
            .AsNoTracking()
            .AnyAsync(roadmap => roadmap.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }
}

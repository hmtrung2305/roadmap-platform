using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

internal static class ContentManagerRoadmapOwnership
{
    public static IQueryable<Roadmap> ApplyScope(
        IQueryable<Roadmap> query,
        Guid actorUserId,
        bool includeAllRoadmaps)
    {
        return includeAllRoadmaps
            ? query
            : query.Where(roadmap => roadmap.OwnerUserId == actorUserId);
    }

    public static void EnsureOwnedByActor(Roadmap roadmap, Guid actorUserId)
    {
        if (actorUserId == Guid.Empty || roadmap.OwnerUserId != actorUserId)
        {
            throw new KeyNotFoundException("Roadmap was not found.");
        }
    }

    public static async Task EnsureVersionOwnedByActorAsync(
        ApplicationDbContext dbContext,
        Guid roadmapVersionId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var isOwned = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .AnyAsync(version =>
                version.RoadmapVersionId == roadmapVersionId
                && version.Roadmap.OwnerUserId == actorUserId,
                cancellationToken);

        if (!isOwned)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }
    }

    public static async Task EnsureNodeOwnedByActorAsync(
        ApplicationDbContext dbContext,
        Guid roadmapNodeId,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var isOwned = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .AnyAsync(node =>
                node.RoadmapNodeId == roadmapNodeId
                && node.RoadmapVersion.Roadmap.OwnerUserId == actorUserId,
                cancellationToken);

        if (!isOwned)
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }
    }
}

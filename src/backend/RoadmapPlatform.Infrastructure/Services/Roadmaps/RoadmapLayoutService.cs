using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapLayoutService(
    ApplicationDbContext dbContext,
    RoadmapDetailBuilder detailBuilder) : IRoadmapLayoutService
{
    private static readonly HashSet<string> ValidLayoutDirections = ["TB", "BT", "LR", "RL"];
    private static readonly HashSet<string> ValidLayoutAlgorithms = ["manual", "dagre", "elk", "custom"];

    public async Task<RoadmapDetailDto> UpdateRoadmapLayoutAsync(
        Guid roadmapVersionId,
        UpdateRoadmapLayoutRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap version was not provided.");
        }

        if (!ValidLayoutDirections.Contains(request.LayoutDirection))
        {
            throw new InvalidOperationException("Invalid layout direction.");
        }

        if (!string.IsNullOrWhiteSpace(request.LayoutAlgorithm) && !ValidLayoutAlgorithms.Contains(request.LayoutAlgorithm))
        {
            throw new InvalidOperationException("Invalid layout algorithm.");
        }

        var version = await dbContext.Set<RoadmapVersion>()
            .Include(v => v.RoadmapNodes)
            .FirstOrDefaultAsync(v => v.RoadmapVersionId == roadmapVersionId, cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        version.LayoutDirection = request.LayoutDirection;
        version.LayoutAlgorithm = string.IsNullOrWhiteSpace(request.LayoutAlgorithm)
            ? null
            : request.LayoutAlgorithm.Trim();

        var nodeById = version.RoadmapNodes.ToDictionary(n => n.RoadmapNodeId);

        foreach (var item in request.Nodes)
        {
            if (!nodeById.TryGetValue(item.RoadmapNodeId, out var node))
            {
                throw new KeyNotFoundException($"Roadmap node was not found: {item.RoadmapNodeId}");
            }

            node.PositionX = item.PositionX;
            node.PositionY = item.PositionY;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await detailBuilder.BuildAsync(roadmapVersionId, null, cancellationToken);
    }
}

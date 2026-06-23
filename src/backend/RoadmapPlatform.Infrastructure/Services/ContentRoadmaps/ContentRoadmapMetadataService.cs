using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.ContentRoadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

public sealed class ContentRoadmapMetadataService(
    ApplicationDbContext dbContext,
    ContentRoadmapQueryService queryService)
{
    public async Task<ContentRoadmapDetailDto> UpdateRoadmapVersionMetadataAsync(
        Guid roadmapVersionId,
        UpdateRoadmapVersionMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var title = ContentRoadmapText.NormalizeRequiredText(request.Title, "Roadmap title is required.");

        var version = await dbContext.Set<RoadmapVersion>()
            .Include(item => item.Roadmap)
            .Where(item => item.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        version.Title = title;
        version.Description = ContentRoadmapText.NormalizeOptionalText(request.Description);
        version.EstimatedTotalHours = request.EstimatedTotalHours;
        version.Roadmap.Title = title;
        version.Roadmap.Description = version.Description;
        version.Roadmap.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(version.RoadmapId, version.RoadmapVersionId, cancellationToken);
    }

    public async Task<ContentRoadmapNodeDto> UpdateRoadmapNodeMetadataAsync(
        Guid roadmapNodeId,
        UpdateRoadmapNodeMetadataRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapNodeId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap node was not provided.", nameof(roadmapNodeId));
        }

        var title = ContentRoadmapText.NormalizeRequiredText(request.Title, "Node title is required.");

        var node = await dbContext.Set<RoadmapNode>()
            .Where(item => item.RoadmapNodeId == roadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (node == null)
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }

        node.Title = title;
        node.Description = ContentRoadmapText.NormalizeOptionalText(request.Description);

        if (ContentRoadmapNodeRules.CanHaveLearningMetadata(node.NodeType))
        {
            node.EstimatedHours = request.EstimatedHours;
            node.DifficultyLevel = ContentRoadmapText.NormalizeOptionalText(request.DifficultyLevel)?.ToLowerInvariant();
        }
        else if (request.EstimatedHours.HasValue || !string.IsNullOrWhiteSpace(request.DifficultyLevel))
        {
            throw new ArgumentException("Only topic and project nodes can have estimated hours or difficulty.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }
}

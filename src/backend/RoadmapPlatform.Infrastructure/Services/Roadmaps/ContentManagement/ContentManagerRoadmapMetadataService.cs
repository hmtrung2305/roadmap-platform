using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapMetadataService(
    ApplicationDbContext dbContext,
    ContentManagerRoadmapQueryService queryService)
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

        var title = ContentManagerRoadmapText.NormalizeRequiredText(request.Title, "Roadmap title is required.");

        var version = await dbContext.Set<RoadmapVersion>()
            .Include(item => item.Roadmap)
            .Where(item => item.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (version == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        version.Title = title;
        version.Description = ContentManagerRoadmapText.NormalizeOptionalText(request.Description);
        version.EstimatedTotalHours = request.EstimatedTotalHours;

        if (version.Status.Equals("published", StringComparison.OrdinalIgnoreCase))
        {
            version.Roadmap.Title = title;
            version.Roadmap.Description = version.Description;
        }

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

        var title = ContentManagerRoadmapText.NormalizeRequiredText(request.Title, "Node title is required.");

        var node = await dbContext.Set<RoadmapNode>()
            .Where(item => item.RoadmapNodeId == roadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (node == null)
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }

        node.Title = title;
        node.Description = ContentManagerRoadmapText.NormalizeOptionalText(request.Description);
        node.Reason = ContentManagerRoadmapText.NormalizeOptionalText(request.Reason);

        if (request.LearningOutcomes != null)
        {
            node.LearningOutcomes = ContentManagerRoadmapNodeContent.SerializeStringArray(request.LearningOutcomes);
        }

        if (request.CompletionCriteria != null)
        {
            node.CompletionCriteria = ContentManagerRoadmapNodeContent.SerializeStringArray(request.CompletionCriteria);
        }

        node.Metadata = ContentManagerRoadmapNodeContent.UpdateMetadata(node.Metadata, node.NodeType, request.Guide);

        if (ContentManagerRoadmapNodeRules.CanHaveLearningMetadata(node.NodeType))
        {
            node.EstimatedHours = request.EstimatedHours;
            node.DifficultyLevel = ContentManagerRoadmapText.NormalizeOptionalText(request.DifficultyLevel)?.ToLowerInvariant();
        }
        else if (request.EstimatedHours.HasValue || !string.IsNullOrWhiteSpace(request.DifficultyLevel))
        {
            throw new ArgumentException("Only topic, choice option, checkpoint, and project nodes can have estimated hours or difficulty.");
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }
}

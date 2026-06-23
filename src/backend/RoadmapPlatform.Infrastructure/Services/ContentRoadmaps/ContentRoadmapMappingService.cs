using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.ContentRoadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.ContentRoadmaps;

public sealed class ContentRoadmapMappingService(
    ApplicationDbContext dbContext,
    ContentRoadmapQueryService queryService)
{
    public async Task<ContentRoadmapNodeDto> AddResourceToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeResourceRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapNodeId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap node was not provided.", nameof(roadmapNodeId));
        }

        if (request.LearningResourceId == Guid.Empty)
        {
            throw new ArgumentException("Learning resource was not provided.", nameof(request));
        }

        var node = await LoadNodeForMappingAsync(roadmapNodeId, cancellationToken);
        ContentRoadmapNodeRules.EnsureNodeSupportsMappings(node);

        var resourceExists = await dbContext.Set<LearningResource>()
            .AnyAsync(resource => resource.LearningResourceId == request.LearningResourceId, cancellationToken);

        if (!resourceExists)
        {
            throw new KeyNotFoundException("Learning resource was not found.");
        }

        var alreadyMapped = await dbContext.Set<RoadmapNodeResource>()
            .AnyAsync(mapping =>
                mapping.RoadmapNodeId == roadmapNodeId
                && mapping.LearningResourceId == request.LearningResourceId,
                cancellationToken);

        if (!alreadyMapped)
        {
            var maxOrder = await dbContext.Set<RoadmapNodeResource>()
                .Where(mapping => mapping.RoadmapNodeId == roadmapNodeId)
                .Select(mapping => (int?)mapping.OrderIndex)
                .MaxAsync(cancellationToken) ?? 0;

            var hasPrimary = await dbContext.Set<RoadmapNodeResource>()
                .AnyAsync(mapping =>
                    mapping.RoadmapNodeId == roadmapNodeId
                    && mapping.IsPrimary,
                    cancellationToken);

            dbContext.Set<RoadmapNodeResource>().Add(new RoadmapNodeResource
            {
                RoadmapNodeId = roadmapNodeId,
                LearningResourceId = request.LearningResourceId,
                OrderIndex = maxOrder + 1,
                IsPrimary = !hasPrimary
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }

    public async Task<ContentRoadmapNodeDto> RemoveResourceFromNodeAsync(
        Guid roadmapNodeId,
        Guid learningResourceId,
        CancellationToken cancellationToken)
    {
        var node = await LoadNodeForMappingAsync(roadmapNodeId, cancellationToken);
        ContentRoadmapNodeRules.EnsureNodeSupportsMappings(node);

        var mapping = await dbContext.Set<RoadmapNodeResource>()
            .Where(item =>
                item.RoadmapNodeId == roadmapNodeId
                && item.LearningResourceId == learningResourceId)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            dbContext.Set<RoadmapNodeResource>().Remove(mapping);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }

    public async Task<ContentRoadmapNodeDto> AddSkillToNodeAsync(
        Guid roadmapNodeId,
        AddRoadmapNodeSkillRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapNodeId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap node was not provided.", nameof(roadmapNodeId));
        }

        if (request.SkillId == Guid.Empty)
        {
            throw new ArgumentException("Skill was not provided.", nameof(request));
        }

        var node = await LoadNodeForMappingAsync(roadmapNodeId, cancellationToken);
        ContentRoadmapNodeRules.EnsureNodeSupportsMappings(node);

        var skillExists = await dbContext.Set<Skill>()
            .AnyAsync(skill => skill.SkillId == request.SkillId, cancellationToken);

        if (!skillExists)
        {
            throw new KeyNotFoundException("Skill was not found.");
        }

        var alreadyMapped = await dbContext.Set<RoadmapNodeSkill>()
            .AnyAsync(mapping =>
                mapping.RoadmapNodeId == roadmapNodeId
                && mapping.SkillId == request.SkillId,
                cancellationToken);

        if (!alreadyMapped)
        {
            dbContext.Set<RoadmapNodeSkill>().Add(new RoadmapNodeSkill
            {
                RoadmapNodeId = roadmapNodeId,
                SkillId = request.SkillId
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }

    public async Task<ContentRoadmapNodeDto> RemoveSkillFromNodeAsync(
        Guid roadmapNodeId,
        Guid skillId,
        CancellationToken cancellationToken)
    {
        var node = await LoadNodeForMappingAsync(roadmapNodeId, cancellationToken);
        ContentRoadmapNodeRules.EnsureNodeSupportsMappings(node);

        var mapping = await dbContext.Set<RoadmapNodeSkill>()
            .Where(item =>
                item.RoadmapNodeId == roadmapNodeId
                && item.SkillId == skillId)
            .FirstOrDefaultAsync(cancellationToken);

        if (mapping != null)
        {
            dbContext.Set<RoadmapNodeSkill>().Remove(mapping);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await queryService.LoadNodeAsync(roadmapNodeId, cancellationToken);
    }

    private async Task<RoadmapNode> LoadNodeForMappingAsync(
        Guid roadmapNodeId,
        CancellationToken cancellationToken)
    {
        var node = await dbContext.Set<RoadmapNode>()
            .Where(item => item.RoadmapNodeId == roadmapNodeId)
            .FirstOrDefaultAsync(cancellationToken);

        return node ?? throw new KeyNotFoundException("Roadmap node was not found.");
    }
}

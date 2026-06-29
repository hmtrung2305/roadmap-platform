using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.Roadmaps;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps.ContentManagement;

public sealed class ContentManagerRoadmapDraftService(
    ApplicationDbContext dbContext,
    ContentManagerRoadmapQueryService queryService,
    ContentManagerRoadmapValidationService validationService)
{
    private const string DraftStatus = "draft";
    private const string PublishedStatus = "published";
    private const string ArchivedStatus = "archived";
    private const string MajorReleaseType = "major";
    private const string InitialReleaseType = "initial";

    public async Task<ContentRoadmapDetailDto> CreateRoadmapAsync(
        CreateRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.CareerRoleId == Guid.Empty)
        {
            throw new ArgumentException("Career role was not provided.", nameof(request.CareerRoleId));
        }

        var careerRoleExists = await dbContext.Set<CareerRole>()
            .AsNoTracking()
            .AnyAsync(role => role.CareerRoleId == request.CareerRoleId, cancellationToken);

        if (!careerRoleExists)
        {
            throw new KeyNotFoundException("Career role was not found.");
        }

        var now = DateTime.UtcNow;
        var title = NormalizeRoadmapTitle(request.Title);
        var description = ContentManagerRoadmapText.NormalizeOptionalText(request.Description);

        var roadmap = new Roadmap
        {
            RoadmapId = Guid.NewGuid(),
            CareerRoleId = request.CareerRoleId,
            OwnerUserId = null,
            Title = title,
            Description = description,
            Visibility = "public",
            CreatedAt = now,
            UpdatedAt = now
        };

        var version = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = roadmap.RoadmapId,
            VersionNumber = 1,
            MajorVersion = 1,
            MinorVersion = 0,
            PatchVersion = 0,
            ReleaseType = InitialReleaseType,
            CreatedFromVersionId = null,
            Status = DraftStatus,
            Title = title,
            Description = description,
            EstimatedTotalHours = request.EstimatedTotalHours,
            LayoutDirection = "TB",
            LayoutAlgorithm = null,
            CreatedByUserId = null,
            PublishedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<Roadmap>().Add(roadmap);
        dbContext.Set<RoadmapVersion>().Add(version);
        AddInitialSampleNodes(version.RoadmapVersionId, now);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(
            roadmap.RoadmapId,
            version.RoadmapVersionId,
            cancellationToken);
    }

    public async Task<ContentRoadmapDetailDto> CloneRoadmapVersionToDraftAsync(
        Guid roadmapVersionId,
        CloneRoadmapVersionDraftRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var sourceVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        if (!sourceVersion.Status.Equals(PublishedStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only published roadmap versions can be copied into a draft.");
        }

        var existingMajorDraft = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.Status == DraftStatus
                && version.ReleaseType == MajorReleaseType
                && version.MinorVersion == 0
                && version.PatchVersion == 0)
            .OrderByDescending(version => version.MajorVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingMajorDraft != null)
        {
            var normalizedDraftTitle = NormalizeRoadmapTitle(existingMajorDraft.Title);
            if (!existingMajorDraft.Title.Equals(normalizedDraftTitle, StringComparison.Ordinal))
            {
                existingMajorDraft.Title = normalizedDraftTitle;
                existingMajorDraft.UpdatedAt = DateTime.UtcNow;
                sourceVersion.Roadmap.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return await queryService.GetRoadmapDetailAsync(
                sourceVersion.RoadmapId,
                existingMajorDraft.RoadmapVersionId,
                cancellationToken);
        }

        var nextMajorVersion = sourceVersion.MajorVersion + 1;

        var conflictingMajorVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.MajorVersion == nextMajorVersion
                && version.MinorVersion == 0
                && version.PatchVersion == 0)
            .FirstOrDefaultAsync(cancellationToken);

        if (conflictingMajorVersion != null)
        {
            throw new InvalidOperationException($"Roadmap version {RoadmapVersionLabels.Format(conflictingMajorVersion)} already exists.");
        }

        var currentMaxVersionNumber = await dbContext.Set<RoadmapVersion>()
            .Where(version => version.RoadmapId == sourceVersion.RoadmapId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var nextVersionNumber = currentMaxVersionNumber + 1;
        var baseTitle = NormalizeRoadmapTitle(ContentManagerRoadmapText.NormalizeOptionalText(request.Title) ?? sourceVersion.Title);

        var draftVersion = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = sourceVersion.RoadmapId,
            VersionNumber = nextVersionNumber,
            MajorVersion = nextMajorVersion,
            MinorVersion = 0,
            PatchVersion = 0,
            ReleaseType = MajorReleaseType,
            CreatedFromVersionId = sourceVersion.RoadmapVersionId,
            Status = DraftStatus,
            Title = baseTitle,
            Description = sourceVersion.Description,
            EstimatedTotalHours = sourceVersion.EstimatedTotalHours,
            LayoutDirection = sourceVersion.LayoutDirection,
            LayoutAlgorithm = sourceVersion.LayoutAlgorithm,
            CreatedByUserId = sourceVersion.CreatedByUserId,
            PublishedAt = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        dbContext.Set<RoadmapVersion>().Add(draftVersion);

        var sourceNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == sourceVersion.RoadmapVersionId)
            .OrderBy(node => node.OrderIndex)
            .ToListAsync(cancellationToken);

        var nodeIdMap = sourceNodes.ToDictionary(node => node.RoadmapNodeId, _ => Guid.NewGuid());

        foreach (var sourceNode in sourceNodes)
        {
            dbContext.Set<RoadmapNode>().Add(CloneNode(sourceNode, draftVersion.RoadmapVersionId, nodeIdMap));
        }

        await CloneEdgesAsync(sourceVersion.RoadmapVersionId, draftVersion.RoadmapVersionId, nodeIdMap, cancellationToken);
        await CloneSkillMappingsAsync(nodeIdMap, cancellationToken);
        await CloneResourceMappingsAsync(nodeIdMap, cancellationToken);

        sourceVersion.Roadmap.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(
            sourceVersion.RoadmapId,
            draftVersion.RoadmapVersionId,
            cancellationToken);
    }

    public async Task<ContentRoadmapDetailDto> PublishRoadmapVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var draftVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (draftVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        EnsureDraftVersion(draftVersion);
        EnsurePublishableStructuralDraftVersion(draftVersion);

        var validation = await validationService.ValidateRoadmapVersionAsync(roadmapVersionId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("The draft has validation errors and cannot be published.");
        }

        var blockingPublishedVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(version =>
                version.RoadmapId == draftVersion.RoadmapId
                && version.Status == PublishedStatus
                && version.RoadmapVersionId != draftVersion.RoadmapVersionId
                && version.MajorVersion >= draftVersion.MajorVersion)
            .OrderByDescending(version => version.MajorVersion)
            .ThenByDescending(version => version.MinorVersion)
            .ThenByDescending(version => version.PatchVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (blockingPublishedVersion != null)
        {
            throw new InvalidOperationException($"A published roadmap version at {RoadmapVersionLabels.Format(blockingPublishedVersion)} already exists.");
        }

        var publishedVersionsToArchive = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == draftVersion.RoadmapId
                && version.Status == PublishedStatus
                && version.RoadmapVersionId != draftVersion.RoadmapVersionId
                && version.MajorVersion < draftVersion.MajorVersion)
            .ToListAsync(cancellationToken);

        foreach (var publishedVersion in publishedVersionsToArchive)
        {
            publishedVersion.Status = ArchivedStatus;
            publishedVersion.UpdatedAt = DateTime.UtcNow;
        }

        draftVersion.Status = PublishedStatus;
        draftVersion.PublishedAt = DateTime.UtcNow;
        draftVersion.UpdatedAt = DateTime.UtcNow;
        draftVersion.Roadmap.Title = draftVersion.Title;
        draftVersion.Roadmap.Description = draftVersion.Description;
        draftVersion.Roadmap.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await queryService.GetRoadmapDetailAsync(
            draftVersion.RoadmapId,
            draftVersion.RoadmapVersionId,
            cancellationToken);
    }

    public async Task DeleteDraftVersionAsync(
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (roadmapVersionId == Guid.Empty)
        {
            throw new ArgumentException("Roadmap version was not provided.", nameof(roadmapVersionId));
        }

        var draftVersion = await dbContext.Set<RoadmapVersion>()
            .Include(version => version.Roadmap)
            .Where(version => version.RoadmapVersionId == roadmapVersionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (draftVersion == null)
        {
            throw new KeyNotFoundException("Roadmap version was not found.");
        }

        EnsureDraftVersion(draftVersion);

        var hasEnrollments = await dbContext.Set<RoadmapEnrollment>()
            .AnyAsync(enrollment => enrollment.RoadmapVersionId == roadmapVersionId, cancellationToken);
        if (hasEnrollments)
        {
            throw new InvalidOperationException("This draft cannot be deleted because it already has learner activity.");
        }

        var nodeIds = await dbContext.Set<RoadmapNode>()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .Select(node => node.RoadmapNodeId)
            .ToListAsync(cancellationToken);

        var nodeSkills = await dbContext.Set<RoadmapNodeSkill>()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var nodeResources = await dbContext.Set<RoadmapNodeResource>()
            .Where(mapping => nodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);
        var edges = await dbContext.Set<RoadmapEdge>()
            .Where(edge => edge.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);
        var nodes = await dbContext.Set<RoadmapNode>()
            .Where(node => node.RoadmapVersionId == roadmapVersionId)
            .ToListAsync(cancellationToken);

        var hasOtherVersions = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .AnyAsync(version =>
                version.RoadmapId == draftVersion.RoadmapId
                && version.RoadmapVersionId != draftVersion.RoadmapVersionId,
                cancellationToken);

        dbContext.Set<RoadmapNodeSkill>().RemoveRange(nodeSkills);
        dbContext.Set<RoadmapNodeResource>().RemoveRange(nodeResources);
        dbContext.Set<RoadmapEdge>().RemoveRange(edges);
        dbContext.Set<RoadmapNode>().RemoveRange(nodes);
        dbContext.Set<RoadmapVersion>().Remove(draftVersion);

        if (!hasOtherVersions
            && draftVersion.ReleaseType.Equals(InitialReleaseType, StringComparison.OrdinalIgnoreCase)
            && draftVersion.CreatedFromVersionId == null)
        {
            dbContext.Set<Roadmap>().Remove(draftVersion.Roadmap);
        }
        else
        {
            draftVersion.Roadmap.UpdatedAt = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddInitialSampleNodes(Guid roadmapVersionId, DateTime now)
    {
        var phaseNodeId = Guid.NewGuid();
        var groupNodeId = Guid.NewGuid();
        var firstTopicNodeId = Guid.NewGuid();
        var secondTopicNodeId = Guid.NewGuid();

        var nodes = new[]
        {
            CreateSampleNode(
                phaseNodeId,
                roadmapVersionId,
                parentNodeId: null,
                slug: "phase-node",
                nodeType: "phase",
                title: "Phase Node",
                orderIndex: 1,
                layoutRole: "trunk",
                isTrackable: false,
                now: now),
            CreateSampleNode(
                groupNodeId,
                roadmapVersionId,
                phaseNodeId,
                slug: "group-node",
                nodeType: "choice_group",
                title: "Group Node",
                orderIndex: 1,
                layoutRole: "side",
                isTrackable: false,
                now: now),
            CreateSampleNode(
                firstTopicNodeId,
                roadmapVersionId,
                groupNodeId,
                slug: "topic-node-1",
                nodeType: "topic",
                title: "Topic Node 1",
                orderIndex: 1,
                layoutRole: "side",
                isTrackable: true,
                now: now),
            CreateSampleNode(
                secondTopicNodeId,
                roadmapVersionId,
                groupNodeId,
                slug: "topic-node-2",
                nodeType: "topic",
                title: "Topic Node 2",
                orderIndex: 2,
                layoutRole: "side",
                isTrackable: true,
                now: now)
        };

        dbContext.Set<RoadmapNode>().AddRange(nodes);
        dbContext.Set<RoadmapEdge>().AddRange(
            CreateSampleEdge(roadmapVersionId, phaseNodeId, groupNodeId, "contains"),
            CreateSampleEdge(roadmapVersionId, groupNodeId, firstTopicNodeId, "choice"),
            CreateSampleEdge(roadmapVersionId, groupNodeId, secondTopicNodeId, "choice"));
    }

    private static RoadmapNode CreateSampleNode(
        Guid roadmapNodeId,
        Guid roadmapVersionId,
        Guid? parentNodeId,
        string slug,
        string nodeType,
        string title,
        int orderIndex,
        string layoutRole,
        bool isTrackable,
        DateTime now)
    {
        return new RoadmapNode
        {
            RoadmapNodeId = roadmapNodeId,
            RoadmapVersionId = roadmapVersionId,
            ParentNodeId = parentNodeId,
            Slug = slug,
            NodeType = nodeType,
            CheckpointType = null,
            SelectionType = nodeType.Equals("choice_group", StringComparison.OrdinalIgnoreCase) ? "complete_all" : null,
            RequiredCount = null,
            Title = title,
            Description = null,
            OrderIndex = orderIndex,
            LayoutRole = layoutRole,
            EstimatedHours = null,
            DifficultyLevel = null,
            Metadata = "{}",
            IsRequired = true,
            IsTrackable = isTrackable,
            LearningOutcomes = "[]",
            CompletionCriteria = "[]",
            CreatedAt = now
        };
    }

    private static RoadmapEdge CreateSampleEdge(
        Guid roadmapVersionId,
        Guid fromNodeId,
        Guid toNodeId,
        string edgeType)
    {
        return new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = roadmapVersionId,
            FromNodeId = fromNodeId,
            ToNodeId = toNodeId,
            EdgeType = edgeType,
            DependencyType = "required",
            Condition = "{}"
        };
    }

    internal static void EnsureDraftVersion(RoadmapVersion version)
    {
        if (!version.Status.Equals(DraftStatus, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only draft roadmap versions can be structurally edited.");
        }
    }

    private static void EnsurePublishableStructuralDraftVersion(RoadmapVersion version)
    {
        var isInitialDraft = version.ReleaseType.Equals(InitialReleaseType, StringComparison.OrdinalIgnoreCase)
            && version.MajorVersion == 1
            && version.MinorVersion == 0
            && version.PatchVersion == 0
            && version.CreatedFromVersionId == null;

        var isMajorDraft = version.ReleaseType.Equals(MajorReleaseType, StringComparison.OrdinalIgnoreCase)
            && version.MinorVersion == 0
            && version.PatchVersion == 0
            && version.CreatedFromVersionId.HasValue;

        if (!isInitialDraft && !isMajorDraft)
        {
            throw new ArgumentException("Only initial and major structural drafts can be published through this workflow.");
        }
    }

    private static string NormalizeRoadmapTitle(string title)
    {
        var baseTitle = ContentManagerRoadmapText.NormalizeRequiredText(title, "Roadmap title is required.");
        return RoadmapVersionLabels.RemoveLegacyVersionSuffix(baseTitle);
    }

    private static RoadmapNode CloneNode(
        RoadmapNode sourceNode,
        Guid draftVersionId,
        IReadOnlyDictionary<Guid, Guid> nodeIdMap)
    {
        return new RoadmapNode
        {
            RoadmapNodeId = nodeIdMap[sourceNode.RoadmapNodeId],
            RoadmapVersionId = draftVersionId,
            ParentNodeId = sourceNode.ParentNodeId.HasValue && nodeIdMap.TryGetValue(sourceNode.ParentNodeId.Value, out var parentId)
                ? parentId
                : null,
            Slug = sourceNode.Slug,
            NodeType = sourceNode.NodeType,
            CheckpointType = sourceNode.CheckpointType,
            SelectionType = sourceNode.SelectionType,
            RequiredCount = sourceNode.RequiredCount,
            Title = sourceNode.Title,
            Description = sourceNode.Description,
            OrderIndex = sourceNode.OrderIndex,
            LayoutRole = sourceNode.LayoutRole,
            EstimatedHours = sourceNode.EstimatedHours,
            DifficultyLevel = sourceNode.DifficultyLevel,
            Metadata = sourceNode.Metadata,
            IsRequired = sourceNode.IsRequired,
            IsTrackable = sourceNode.IsTrackable,
            LearningOutcomes = sourceNode.LearningOutcomes,
            CompletionCriteria = sourceNode.CompletionCriteria,
            CreatedAt = DateTime.UtcNow
        };
    }

    private async Task CloneEdgesAsync(
        Guid sourceVersionId,
        Guid draftVersionId,
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceEdges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == sourceVersionId)
            .ToListAsync(cancellationToken);

        foreach (var sourceEdge in sourceEdges)
        {
            if (!nodeIdMap.TryGetValue(sourceEdge.FromNodeId, out var fromNodeId)
                || !nodeIdMap.TryGetValue(sourceEdge.ToNodeId, out var toNodeId))
            {
                continue;
            }

            dbContext.Set<RoadmapEdge>().Add(new RoadmapEdge
            {
                RoadmapEdgeId = Guid.NewGuid(),
                RoadmapVersionId = draftVersionId,
                FromNodeId = fromNodeId,
                ToNodeId = toNodeId,
                EdgeType = sourceEdge.EdgeType,
                DependencyType = sourceEdge.DependencyType,
                Condition = sourceEdge.Condition
            });
        }
    }

    private async Task CloneSkillMappingsAsync(
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceNodeIds = nodeIdMap.Keys.ToList();
        var mappings = await dbContext.Set<RoadmapNodeSkill>()
            .AsNoTracking()
            .Where(mapping => sourceNodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            dbContext.Set<RoadmapNodeSkill>().Add(new RoadmapNodeSkill
            {
                RoadmapNodeSkillId = Guid.NewGuid(),
                RoadmapNodeId = nodeIdMap[mapping.RoadmapNodeId],
                SkillId = mapping.SkillId
            });
        }
    }

    private async Task CloneResourceMappingsAsync(
        IReadOnlyDictionary<Guid, Guid> nodeIdMap,
        CancellationToken cancellationToken)
    {
        var sourceNodeIds = nodeIdMap.Keys.ToList();
        var mappings = await dbContext.Set<RoadmapNodeResource>()
            .AsNoTracking()
            .Where(mapping => sourceNodeIds.Contains(mapping.RoadmapNodeId))
            .ToListAsync(cancellationToken);

        foreach (var mapping in mappings)
        {
            dbContext.Set<RoadmapNodeResource>().Add(new RoadmapNodeResource
            {
                RoadmapNodeResourceId = Guid.NewGuid(),
                RoadmapNodeId = nodeIdMap[mapping.RoadmapNodeId],
                LearningResourceId = mapping.LearningResourceId
            });
        }
    }
}

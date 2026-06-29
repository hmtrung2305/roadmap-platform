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
    private const string PatchReleaseType = "patch";

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


    public async Task<ContentRoadmapDetailDto> CreatePatchRoadmapVersionDraftAsync(
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
            throw new ArgumentException("Only published roadmap versions can be patched.");
        }

        var existingPatchDraft = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.Status == DraftStatus
                && version.ReleaseType == PatchReleaseType
                && version.MajorVersion == sourceVersion.MajorVersion
                && version.MinorVersion == sourceVersion.MinorVersion)
            .OrderByDescending(version => version.PatchVersion)
            .ThenByDescending(version => version.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPatchDraft != null)
        {
            return await queryService.GetRoadmapDetailAsync(
                sourceVersion.RoadmapId,
                existingPatchDraft.RoadmapVersionId,
                cancellationToken);
        }

        var currentMaxPatchVersion = await dbContext.Set<RoadmapVersion>()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.MajorVersion == sourceVersion.MajorVersion
                && version.MinorVersion == sourceVersion.MinorVersion)
            .Select(version => (int?)version.PatchVersion)
            .MaxAsync(cancellationToken) ?? sourceVersion.PatchVersion;
        var nextPatchVersion = Math.Max(currentMaxPatchVersion, sourceVersion.PatchVersion) + 1;

        var conflictingPatchVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(version =>
                version.RoadmapId == sourceVersion.RoadmapId
                && version.MajorVersion == sourceVersion.MajorVersion
                && version.MinorVersion == sourceVersion.MinorVersion
                && version.PatchVersion == nextPatchVersion)
            .FirstOrDefaultAsync(cancellationToken);

        if (conflictingPatchVersion != null)
        {
            throw new InvalidOperationException($"Roadmap version {RoadmapVersionLabels.Format(conflictingPatchVersion)} already exists.");
        }

        var currentMaxVersionNumber = await dbContext.Set<RoadmapVersion>()
            .Where(version => version.RoadmapId == sourceVersion.RoadmapId)
            .Select(version => (int?)version.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var nextVersionNumber = currentMaxVersionNumber + 1;
        var baseTitle = NormalizeRoadmapTitle(ContentManagerRoadmapText.NormalizeOptionalText(request.Title) ?? sourceVersion.Title);
        var now = DateTime.UtcNow;

        var draftVersion = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = sourceVersion.RoadmapId,
            VersionNumber = nextVersionNumber,
            MajorVersion = sourceVersion.MajorVersion,
            MinorVersion = sourceVersion.MinorVersion,
            PatchVersion = nextPatchVersion,
            ReleaseType = PatchReleaseType,
            CreatedFromVersionId = sourceVersion.RoadmapVersionId,
            Status = DraftStatus,
            Title = baseTitle,
            Description = sourceVersion.Description,
            EstimatedTotalHours = sourceVersion.EstimatedTotalHours,
            LayoutDirection = sourceVersion.LayoutDirection,
            LayoutAlgorithm = sourceVersion.LayoutAlgorithm,
            CreatedByUserId = sourceVersion.CreatedByUserId,
            PublishedAt = null,
            CreatedAt = now,
            UpdatedAt = now
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

        sourceVersion.Roadmap.UpdatedAt = now;

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
        EnsurePublishableDraftVersion(draftVersion);

        var validation = await validationService.ValidateRoadmapVersionAsync(roadmapVersionId, cancellationToken);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException("The draft has validation errors and cannot be published.");
        }

        if (IsPatchDraft(draftVersion))
        {
            return await PublishPatchDraftVersionAsync(draftVersion, cancellationToken);
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

    private async Task<ContentRoadmapDetailDto> PublishPatchDraftVersionAsync(
        RoadmapVersion draftVersion,
        CancellationToken cancellationToken)
    {
        if (!draftVersion.CreatedFromVersionId.HasValue)
        {
            throw new ArgumentException("Patch drafts must reference the published version they patch.");
        }

        var sourceVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(version => version.RoadmapVersionId == draftVersion.CreatedFromVersionId.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (sourceVersion == null)
        {
            throw new KeyNotFoundException("Source roadmap version was not found.");
        }

        if (sourceVersion.RoadmapId != draftVersion.RoadmapId
            || sourceVersion.MajorVersion != draftVersion.MajorVersion
            || sourceVersion.MinorVersion != draftVersion.MinorVersion
            || sourceVersion.PatchVersion >= draftVersion.PatchVersion)
        {
            throw new ArgumentException("Patch drafts must stay on the same major/minor version line and increment the patch number.");
        }

        var blockingPublishedVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .Where(version =>
                version.RoadmapId == draftVersion.RoadmapId
                && version.Status == PublishedStatus
                && version.RoadmapVersionId != draftVersion.RoadmapVersionId
                && version.MajorVersion == draftVersion.MajorVersion
                && version.MinorVersion == draftVersion.MinorVersion
                && version.PatchVersion >= draftVersion.PatchVersion)
            .OrderByDescending(version => version.PatchVersion)
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
                && version.MajorVersion == draftVersion.MajorVersion
                && version.MinorVersion == draftVersion.MinorVersion
                && version.PatchVersion < draftVersion.PatchVersion)
            .ToListAsync(cancellationToken);

        var archivedVersionIds = publishedVersionsToArchive
            .Select(version => version.RoadmapVersionId)
            .ToList();

        var enrollmentsToMove = archivedVersionIds.Count == 0
            ? new List<RoadmapEnrollment>()
            : await dbContext.Set<RoadmapEnrollment>()
                .Where(enrollment => archivedVersionIds.Contains(enrollment.RoadmapVersionId))
                .ToListAsync(cancellationToken);

        await RemapEnrollmentsToPatchVersionAsync(enrollmentsToMove, draftVersion.RoadmapVersionId, cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var publishedVersion in publishedVersionsToArchive)
        {
            publishedVersion.Status = ArchivedStatus;
            publishedVersion.UpdatedAt = now;
        }

        draftVersion.Status = PublishedStatus;
        draftVersion.PublishedAt = now;
        draftVersion.UpdatedAt = now;
        draftVersion.Roadmap.Title = draftVersion.Title;
        draftVersion.Roadmap.Description = draftVersion.Description;
        draftVersion.Roadmap.UpdatedAt = now;

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

    private async Task RemapEnrollmentsToPatchVersionAsync(
        IReadOnlyCollection<RoadmapEnrollment> enrollments,
        Guid targetRoadmapVersionId,
        CancellationToken cancellationToken)
    {
        if (enrollments.Count == 0)
        {
            return;
        }

        var sourceVersionIds = enrollments
            .Select(enrollment => enrollment.RoadmapVersionId)
            .Distinct()
            .ToList();
        var enrollmentIds = enrollments
            .Select(enrollment => enrollment.RoadmapEnrollmentId)
            .ToList();

        var targetNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == targetRoadmapVersionId)
            .ToListAsync(cancellationToken);
        var targetNodesByKey = targetNodes
            .GroupBy(GetPatchNodeIdentityKey)
            .ToDictionary(group => group.Key, group => group.First().RoadmapNodeId);

        var sourceNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => sourceVersionIds.Contains(node.RoadmapVersionId))
            .ToListAsync(cancellationToken);
        var nodeIdMap = new Dictionary<Guid, Guid>();
        foreach (var sourceNode in sourceNodes)
        {
            if (targetNodesByKey.TryGetValue(GetPatchNodeIdentityKey(sourceNode), out var targetNodeId))
            {
                nodeIdMap[sourceNode.RoadmapNodeId] = targetNodeId;
            }
        }

        var progressRows = await dbContext.Set<UserNodeProgress>()
            .Where(progress => enrollmentIds.Contains(progress.RoadmapEnrollmentId))
            .ToListAsync(cancellationToken);
        foreach (var progress in progressRows)
        {
            if (nodeIdMap.TryGetValue(progress.RoadmapNodeId, out var targetNodeId))
            {
                progress.RoadmapNodeId = targetNodeId;
            }
        }

        var progressEvents = await dbContext.Set<ProgressEvent>()
            .Where(progressEvent => enrollmentIds.Contains(progressEvent.RoadmapEnrollmentId))
            .ToListAsync(cancellationToken);
        foreach (var progressEvent in progressEvents)
        {
            if (nodeIdMap.TryGetValue(progressEvent.RoadmapNodeId, out var targetNodeId))
            {
                progressEvent.RoadmapNodeId = targetNodeId;
            }
        }

        var now = DateTime.UtcNow;
        foreach (var enrollment in enrollments)
        {
            enrollment.RoadmapVersionId = targetRoadmapVersionId;
            enrollment.UpdatedAt = now;
        }
    }

    private static string GetPatchNodeIdentityKey(RoadmapNode node)
    {
        return string.Join("::",
            ContentManagerRoadmapStructureRules.NormalizeNodeType(node.NodeType),
            node.Slug.Trim().ToLowerInvariant());
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
            throw new ArgumentException("Only draft roadmap versions can be edited.");
        }
    }

    internal static bool IsPatchDraft(RoadmapVersion version)
    {
        return version.Status.Equals(DraftStatus, StringComparison.OrdinalIgnoreCase)
            && version.ReleaseType.Equals(PatchReleaseType, StringComparison.OrdinalIgnoreCase);
    }

    internal static void EnsureStructuralDraftVersion(RoadmapVersion version)
    {
        EnsureDraftVersion(version);

        if (IsPatchDraft(version))
        {
            throw new ArgumentException("Patch drafts only allow safe content edits. Structural edits are not allowed.");
        }
    }

    private static void EnsurePublishableDraftVersion(RoadmapVersion version)
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

        var isPatchDraft = IsPatchDraft(version);

        if (!isInitialDraft && !isMajorDraft && !isPatchDraft)
        {
            throw new ArgumentException("Only initial, major, and patch drafts can be published through this workflow.");
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

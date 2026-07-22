using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapEnrollmentService(ApplicationDbContext dbContext) : IRoadmapEnrollmentService
{
    private const string PublishedStatus = "published";
    private const string MajorReleaseType = "major";
    private const string MinorReleaseType = "minor";
    private const string PatchReleaseType = "patch";

    public async Task<RoadmapEnrollmentDto> EnrollAsync(
        Guid userId,
        EnrollRoadmapRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.RoadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap version was not provided.");
        }

        var versionExists = await dbContext.Set<RoadmapVersion>()
            .AnyAsync(v =>
                v.RoadmapVersionId == request.RoadmapVersionId &&
                v.Status == "published",
                cancellationToken);

        if (!versionExists)
        {
            throw new KeyNotFoundException("Published roadmap version was not found.");
        }

        var existingEnrollment = await dbContext.Set<RoadmapEnrollment>()
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.RoadmapVersionId == request.RoadmapVersionId,
                cancellationToken);

        if (existingEnrollment != null)
        {
            return RoadmapDetailBuilder.MapEnrollment(existingEnrollment);
        }

        var now = DateTime.UtcNow;
        var enrollment = new RoadmapEnrollment
        {
            RoadmapEnrollmentId = Guid.NewGuid(),
            UserId = userId,
            RoadmapVersionId = request.RoadmapVersionId,
            Status = "active",
            ProgressPercent = 0,
            StartedAt = now,
            UpdatedAt = now
        };

        dbContext.Set<RoadmapEnrollment>().Add(enrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RoadmapDetailBuilder.MapEnrollment(enrollment);
    }

    public async Task<RoadmapEnrollmentDto?> GetCurrentEnrollmentAsync(
        Guid userId,
        Guid roadmapVersionId,
        CancellationToken cancellationToken)
    {
        var enrollment = await dbContext.Set<RoadmapEnrollment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e =>
                e.UserId == userId &&
                e.RoadmapVersionId == roadmapVersionId,
                cancellationToken);

        return enrollment == null ? null : RoadmapDetailBuilder.MapEnrollment(enrollment);
    }

    public async Task<RoadmapEnrollmentDto> MigrateEnrollmentAsync(
        Guid userId,
        Guid roadmapEnrollmentId,
        MigrateRoadmapEnrollmentRequestDto request,
        CancellationToken cancellationToken)
    {
        if (roadmapEnrollmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Roadmap enrollment was not provided.");
        }

        if (request == null || request.TargetRoadmapVersionId == Guid.Empty)
        {
            throw new InvalidOperationException("Target roadmap version was not provided.");
        }

        var enrollment = await dbContext.Set<RoadmapEnrollment>()
            .Include(item => item.RoadmapVersion)
            .FirstOrDefaultAsync(item =>
                item.RoadmapEnrollmentId == roadmapEnrollmentId &&
                item.UserId == userId,
                cancellationToken);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Roadmap enrollment was not found.");
        }

        if (enrollment.RoadmapVersionId == request.TargetRoadmapVersionId)
        {
            return RoadmapDetailBuilder.MapEnrollment(enrollment);
        }

        var targetVersion = await dbContext.Set<RoadmapVersion>()
            .AsNoTracking()
            .FirstOrDefaultAsync(version =>
                version.RoadmapVersionId == request.TargetRoadmapVersionId &&
                version.Status == PublishedStatus,
                cancellationToken);

        if (targetVersion == null)
        {
            throw new KeyNotFoundException("Published target roadmap version was not found.");
        }

        if (targetVersion.RoadmapId != enrollment.RoadmapVersion.RoadmapId)
        {
            throw new InvalidOperationException("Target version must belong to the same roadmap.");
        }

        var migrationType = RoadmapVersionLabels.GetChangeType(targetVersion, enrollment.RoadmapVersion);

        if (migrationType == null)
        {
            throw new InvalidOperationException("Target roadmap version must be newer than the current enrollment version.");
        }

        if (migrationType == PatchReleaseType)
        {
            throw new InvalidOperationException("Patch updates are applied automatically.");
        }

        if (migrationType != MajorReleaseType && migrationType != MinorReleaseType)
        {
            throw new InvalidOperationException("Only major and minor roadmap updates can be migrated manually.");
        }

        var existingTargetEnrollment = await dbContext.Set<RoadmapEnrollment>()
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.UserId == userId &&
                item.RoadmapVersionId == targetVersion.RoadmapVersionId,
                cancellationToken);

        if (existingTargetEnrollment != null)
        {
            return RoadmapDetailBuilder.MapEnrollment(existingTargetEnrollment);
        }

        await RemapEnrollmentProgressAsync(enrollment, targetVersion.RoadmapVersionId, cancellationToken);

        var now = DateTime.UtcNow;
        enrollment.RoadmapVersionId = targetVersion.RoadmapVersionId;
        enrollment.UpdatedAt = now;

        await RecalculateEnrollmentAsync(enrollment, targetVersion.RoadmapVersionId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return RoadmapDetailBuilder.MapEnrollment(enrollment);
    }

    private async Task RemapEnrollmentProgressAsync(
        RoadmapEnrollment enrollment,
        Guid targetRoadmapVersionId,
        CancellationToken cancellationToken)
    {
        var sourceNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == enrollment.RoadmapVersionId)
            .ToListAsync(cancellationToken);
        var targetNodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == targetRoadmapVersionId)
            .ToListAsync(cancellationToken);

        var sourceNodesById = sourceNodes.ToDictionary(node => node.RoadmapNodeId);
        var targetNodesByKey = targetNodes
            .GroupBy(GetNodeIdentityKey)
            .ToDictionary(group => group.Key, group => group.First().RoadmapNodeId);

        var progressRows = await dbContext.Set<UserNodeProgress>()
            .Where(progress => progress.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
            .ToListAsync(cancellationToken);
        var progressRowsToRemove = new List<UserNodeProgress>();

        foreach (var progress in progressRows)
        {
            if (!sourceNodesById.TryGetValue(progress.RoadmapNodeId, out var sourceNode)
                || !targetNodesByKey.TryGetValue(GetNodeIdentityKey(sourceNode), out var targetNodeId))
            {
                progressRowsToRemove.Add(progress);
                continue;
            }

            progress.RoadmapNodeId = targetNodeId;
        }

        if (progressRowsToRemove.Count > 0)
        {
            dbContext.Set<UserNodeProgress>().RemoveRange(progressRowsToRemove);
        }

        var progressEvents = await dbContext.Set<ProgressEvent>()
            .Where(progressEvent => progressEvent.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
            .ToListAsync(cancellationToken);

        foreach (var progressEvent in progressEvents)
        {
            if (sourceNodesById.TryGetValue(progressEvent.RoadmapNodeId, out var sourceNode)
                && targetNodesByKey.TryGetValue(GetNodeIdentityKey(sourceNode), out var targetNodeId))
            {
                progressEvent.RoadmapNodeId = targetNodeId;
            }
        }
    }

    private async Task RecalculateEnrollmentAsync(
        RoadmapEnrollment enrollment,
        Guid targetRoadmapVersionId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == targetRoadmapVersionId)
            .ToListAsync(cancellationToken);
        var edges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == targetRoadmapVersionId)
            .ToListAsync(cancellationToken);
        var progressRows = await dbContext.Set<UserNodeProgress>()
            .Where(progress => progress.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
            .ToListAsync(cancellationToken);
        var targetNodeIds = nodes
            .Select(node => node.RoadmapNodeId)
            .ToHashSet();
        var progressByNodeId = progressRows
            .Where(progress => targetNodeIds.Contains(progress.RoadmapNodeId))
            .GroupBy(progress => progress.RoadmapNodeId)
            .ToDictionary(group => group.Key, group => group.First());
        var statusByNodeId = RoadmapProgressCalculator.CalculateStatuses(nodes, edges, progressByNodeId);
        var summary = RoadmapProgressCalculator.CalculateRoadmapProgress(nodes, edges, statusByNodeId);

        enrollment.ProgressPercent = summary.ProgressPercent;
        enrollment.Status = summary.TotalUnits > 0 && summary.CompletedUnits == summary.TotalUnits
            ? "completed"
            : "active";
        enrollment.CompletedAt = enrollment.Status == "completed" ? now : null;
    }

    private static string GetNodeIdentityKey(RoadmapNode node)
    {
        return string.Join("::",
            node.NodeType.Trim().ToLowerInvariant(),
            node.Slug.Trim().ToLowerInvariant());
    }
}

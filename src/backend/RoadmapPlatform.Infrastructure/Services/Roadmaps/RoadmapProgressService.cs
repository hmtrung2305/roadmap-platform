using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Interfaces.Roadmaps;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.Roadmaps;

public sealed class RoadmapProgressService(ApplicationDbContext dbContext) : IRoadmapProgressService
{
    private static readonly HashSet<string> ValidStoredStatuses =
    [
        "pending",
        "in_progress",
        "completed",
        "skipped"
    ];

    public async Task<UpdateNodeProgressResultDto> UpdateNodeProgressAsync(
        Guid userId,
        Guid roadmapEnrollmentId,
        Guid roadmapNodeId,
        UpdateNodeProgressRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!ValidStoredStatuses.Contains(request.Status))
        {
            throw new InvalidOperationException("Invalid node progress status.");
        }

        var enrollment = await dbContext.Set<RoadmapEnrollment>()
            .FirstOrDefaultAsync(e =>
                e.RoadmapEnrollmentId == roadmapEnrollmentId &&
                e.UserId == userId,
                cancellationToken);

        if (enrollment == null)
        {
            throw new KeyNotFoundException("Roadmap enrollment was not found.");
        }

        var snapshot = await LoadProgressSnapshotAsync(enrollment, cancellationToken);

        if (!snapshot.NodeById.TryGetValue(roadmapNodeId, out var node))
        {
            throw new KeyNotFoundException("Roadmap node was not found.");
        }

        if (!RoadmapProgressCalculator.IsManualProgressNode(node))
        {
            throw new InvalidOperationException("This roadmap node has computed progress and cannot be updated manually.");
        }

        if (request.Status == "skipped" && node.IsRequired)
        {
            throw new InvalidOperationException("Required roadmap nodes cannot be skipped.");
        }

        var beforeStatusByNodeId = RoadmapProgressCalculator.CalculateStatuses(
            snapshot.Nodes,
            snapshot.Edges,
            snapshot.ProgressByNodeId);

        if (beforeStatusByNodeId.GetValueOrDefault(roadmapNodeId) == "locked")
        {
            throw new InvalidOperationException("This roadmap node is still locked.");
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var duplicateEventExists = await dbContext.Set<ProgressEvent>()
                .AnyAsync(e =>
                    e.RoadmapEnrollmentId == roadmapEnrollmentId &&
                    e.IdempotencyKey == request.IdempotencyKey,
                    cancellationToken);

            if (duplicateEventExists)
            {
                return BuildResult(enrollment, snapshot, beforeStatusByNodeId, beforeStatusByNodeId, [roadmapNodeId]);
            }
        }

        snapshot.ProgressByNodeId.TryGetValue(roadmapNodeId, out var progress);
        var oldStatus = progress?.Status;
        var now = DateTime.UtcNow;

        if (progress == null)
        {
            progress = new UserNodeProgress
            {
                UserNodeProgressId = Guid.NewGuid(),
                RoadmapEnrollmentId = roadmapEnrollmentId,
                RoadmapNodeId = roadmapNodeId,
                Status = "pending",
                UpdatedAt = now
            };

            dbContext.Set<UserNodeProgress>().Add(progress);
            snapshot.ProgressRows.Add(progress);
            snapshot.ProgressByNodeId[roadmapNodeId] = progress;
        }

        progress.Status = request.Status;
        progress.UpdatedAt = now;

        ApplyProgressTimestamps(progress, request.Status, now);

        dbContext.Set<ProgressEvent>().Add(new ProgressEvent
        {
            ProgressEventId = Guid.NewGuid(),
            RoadmapEnrollmentId = roadmapEnrollmentId,
            RoadmapNodeId = roadmapNodeId,
            UserId = userId,
            OldStatus = oldStatus,
            NewStatus = request.Status,
            IdempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? null
                : request.IdempotencyKey.Trim(),
            CreatedAt = now
        });

        var afterStatusByNodeId = RoadmapProgressCalculator.CalculateStatuses(
            snapshot.Nodes,
            snapshot.Edges,
            snapshot.ProgressByNodeId);

        var progressSummary = RoadmapProgressCalculator.CalculateRoadmapProgress(
            snapshot.Nodes,
            snapshot.Edges,
            afterStatusByNodeId);

        enrollment.ProgressPercent = progressSummary.ProgressPercent;
        enrollment.Status = progressSummary.TotalUnits > 0 && progressSummary.CompletedUnits == progressSummary.TotalUnits
            ? "completed"
            : "active";
        enrollment.CompletedAt = enrollment.Status == "completed" ? now : null;
        enrollment.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        return BuildResult(enrollment, snapshot, beforeStatusByNodeId, afterStatusByNodeId, [roadmapNodeId]);
    }

    private async Task<ProgressSnapshot> LoadProgressSnapshotAsync(
        RoadmapEnrollment enrollment,
        CancellationToken cancellationToken)
    {
        var nodes = await dbContext.Set<RoadmapNode>()
            .AsNoTracking()
            .Where(n => n.RoadmapVersionId == enrollment.RoadmapVersionId)
            .ToListAsync(cancellationToken);

        var edges = await dbContext.Set<RoadmapEdge>()
            .AsNoTracking()
            .Where(e => e.RoadmapVersionId == enrollment.RoadmapVersionId)
            .ToListAsync(cancellationToken);

        var progressRows = await dbContext.Set<UserNodeProgress>()
            .Where(p => p.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
            .ToListAsync(cancellationToken);

        return new ProgressSnapshot
        {
            Nodes = nodes,
            Edges = edges,
            ProgressRows = progressRows,
            NodeById = nodes.ToDictionary(n => n.RoadmapNodeId),
            ProgressByNodeId = progressRows.ToDictionary(p => p.RoadmapNodeId)
        };
    }

    private static UpdateNodeProgressResultDto BuildResult(
        RoadmapEnrollment enrollment,
        ProgressSnapshot snapshot,
        IReadOnlyDictionary<Guid, string> beforeStatusByNodeId,
        IReadOnlyDictionary<Guid, string> afterStatusByNodeId,
        IReadOnlyCollection<Guid> forceIncludeNodeIds)
    {
        var progressSummary = RoadmapProgressCalculator.CalculateRoadmapProgress(
            snapshot.Nodes,
            snapshot.Edges,
            afterStatusByNodeId);

        var changedNodeIds = snapshot.Nodes
            .Select(n => n.RoadmapNodeId)
            .Where(id =>
                forceIncludeNodeIds.Contains(id) ||
                beforeStatusByNodeId.GetValueOrDefault(id, "pending") != afterStatusByNodeId.GetValueOrDefault(id, "pending"))
            .ToHashSet();

        return new UpdateNodeProgressResultDto
        {
            Enrollment = RoadmapDetailBuilder.MapEnrollment(enrollment),
            TrackableNodeCount = progressSummary.TotalUnits,
            CompletedNodeCount = progressSummary.CompletedUnits,
            ProgressPercent = progressSummary.ProgressPercent,
            ChangedNodes = snapshot.Nodes
                .Where(n => changedNodeIds.Contains(n.RoadmapNodeId))
                .OrderBy(n => n.OrderIndex)
                .Select(n => MapNodeProgress(
                    n.RoadmapNodeId,
                    snapshot.ProgressByNodeId.GetValueOrDefault(n.RoadmapNodeId),
                    afterStatusByNodeId.GetValueOrDefault(n.RoadmapNodeId, "pending")))
                .ToList()
        };
    }

    private static UserNodeProgressDto MapNodeProgress(
        Guid roadmapNodeId,
        UserNodeProgress? progress,
        string effectiveStatus)
    {
        if (progress == null)
        {
            return new UserNodeProgressDto
            {
                RoadmapNodeId = roadmapNodeId,
                Status = effectiveStatus,
                IsComputed = true
            };
        }

        return new UserNodeProgressDto
        {
            UserNodeProgressId = progress.UserNodeProgressId,
            RoadmapEnrollmentId = progress.RoadmapEnrollmentId,
            RoadmapNodeId = progress.RoadmapNodeId,
            Status = effectiveStatus,
            IsComputed = progress.Status != effectiveStatus,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            SkippedAt = progress.SkippedAt,
            UpdatedAt = progress.UpdatedAt
        };
    }

    private static void ApplyProgressTimestamps(UserNodeProgress progress, string status, DateTime now)
    {
        switch (status)
        {
            case "pending":
                progress.StartedAt = null;
                progress.CompletedAt = null;
                progress.SkippedAt = null;
                break;
            case "in_progress":
                progress.StartedAt ??= now;
                progress.CompletedAt = null;
                progress.SkippedAt = null;
                break;
            case "completed":
                progress.StartedAt ??= now;
                progress.CompletedAt = now;
                progress.SkippedAt = null;
                break;
            case "skipped":
                progress.StartedAt ??= now;
                progress.CompletedAt = null;
                progress.SkippedAt = now;
                break;
        }
    }

    private sealed class ProgressSnapshot
    {
        public List<RoadmapNode> Nodes { get; init; } = [];
        public List<RoadmapEdge> Edges { get; init; } = [];
        public List<UserNodeProgress> ProgressRows { get; init; } = [];
        public Dictionary<Guid, RoadmapNode> NodeById { get; init; } = [];
        public Dictionary<Guid, UserNodeProgress> ProgressByNodeId { get; init; } = [];
    }
}

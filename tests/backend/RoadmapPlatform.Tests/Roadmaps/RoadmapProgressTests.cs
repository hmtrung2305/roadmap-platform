using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.Roadmaps;

public sealed class RoadmapProgressTests
{
    [Fact]
    public async Task TC106_UnlockedNodeCanBeStarted()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var setup = await CreateTwoStepRoadmapAsync(fixture);

        var result = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            setup.Enrollment.RoadmapEnrollmentId,
            setup.First.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "in_progress" },
            CancellationToken.None);

        Assert.Contains(result.ChangedNodes, node =>
            node.RoadmapNodeId == setup.First.RoadmapNodeId && node.Status == "in_progress");
        Assert.Equal("active", result.Enrollment.Status);
        Assert.Equal(0m, result.ProgressPercent);
        var progress = await fixture.Context.UserNodeProgresses.AsNoTracking().SingleAsync();
        Assert.Equal(setup.Enrollment.RoadmapEnrollmentId, progress.RoadmapEnrollmentId);
        Assert.Equal(setup.First.RoadmapNodeId, progress.RoadmapNodeId);
        Assert.Equal("in_progress", progress.Status);
        Assert.NotNull(progress.StartedAt);
    }

    [Fact]
    public async Task TC107_LockedNodeCannotBeStartedBeforeItsRequiredDependency()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var setup = await CreateTwoStepRoadmapAsync(fixture);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.ProgressService.UpdateNodeProgressAsync(
                fixture.Learner.UserId,
                setup.Enrollment.RoadmapEnrollmentId,
                setup.Second.RoadmapNodeId,
                new UpdateNodeProgressRequestDto { Status = "in_progress" },
                CancellationToken.None));

        Assert.Contains("locked", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await fixture.Context.UserNodeProgresses.AnyAsync());
        Assert.False(await fixture.Context.ProgressEvents.AnyAsync());
        var graph = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            setup.Enrollment.RoadmapVersion.Roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        Assert.Equal("locked", graph.Nodes.Single(node => node.RoadmapNodeId == setup.Second.RoadmapNodeId).Progress.Status);
    }

    [Fact]
    public async Task TC108_CompletingDependencyUnlocksDependentNodeAndUpdatesProgressOnce()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var setup = await CreateTwoStepRoadmapAsync(fixture);

        var started = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            setup.Enrollment.RoadmapEnrollmentId,
            setup.First.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "in_progress" },
            CancellationToken.None);
        Assert.Equal(0m, started.ProgressPercent);

        var result = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            setup.Enrollment.RoadmapEnrollmentId,
            setup.First.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "completed" },
            CancellationToken.None);

        Assert.Equal(50m, result.ProgressPercent);
        Assert.Contains(result.ChangedNodes, node =>
            node.RoadmapNodeId == setup.Second.RoadmapNodeId && node.Status == "pending");
        var events = await fixture.Context.ProgressEvents
            .AsNoTracking()
            .OrderBy(item => item.CreatedAt)
            .ToListAsync();
        Assert.Equal(2, events.Count);
        Assert.Null(events[0].OldStatus);
        Assert.Equal("in_progress", events[0].NewStatus);
        Assert.Equal("in_progress", events[1].OldStatus);
        Assert.Equal("completed", events[1].NewStatus);
        Assert.Single(events, item => item.NewStatus == "completed");
        var graph = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            setup.Enrollment.RoadmapVersion.Roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        Assert.Equal("completed", graph.Nodes.Single(node => node.RoadmapNodeId == setup.First.RoadmapNodeId).Progress.Status);
        Assert.Equal("pending", graph.Nodes.Single(node => node.RoadmapNodeId == setup.Second.RoadmapNodeId).Progress.Status);
        Assert.Equal(50m, graph.ProgressPercent);
        Assert.Single(await fixture.Context.UserNodeProgresses.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task TC109_OptionalNodeCanBeSkippedAndDownstreamRulesAreRecalculated()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var optional = fixture.AddNode(
            version,
            "project",
            "optional",
            "Optional",
            phase,
            isRequired: false,
            orderIndex: 1);
        var downstream = fixture.AddNode(
            version,
            "project",
            "downstream",
            "Downstream Required",
            phase,
            isRequired: true,
            orderIndex: 2);
        fixture.AddEdge(version, optional, downstream, "dependency", "required");
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();

        var graphBefore = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        Assert.Equal(
            "locked",
            graphBefore.Nodes.Single(node => node.RoadmapNodeId == downstream.RoadmapNodeId).Progress.Status);

        var result = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            optional.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "skipped" },
            CancellationToken.None);

        Assert.Equal(0m, result.ProgressPercent);
        Assert.Equal(
            "skipped",
            result.ChangedNodes.Single(node => node.RoadmapNodeId == optional.RoadmapNodeId).Status);
        var stored = await fixture.Context.UserNodeProgresses.AsNoTracking().SingleAsync();
        Assert.Equal(optional.RoadmapNodeId, stored.RoadmapNodeId);
        Assert.Equal("skipped", stored.Status);
        Assert.NotNull(stored.SkippedAt);
        var graphAfter = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        Assert.Equal(
            "skipped",
            graphAfter.Nodes.Single(node => node.RoadmapNodeId == optional.RoadmapNodeId).Progress.Status);
        Assert.Equal(
            "locked",
            graphAfter.Nodes.Single(node => node.RoadmapNodeId == downstream.RoadmapNodeId).Progress.Status);
        Assert.Equal(0m, graphAfter.ProgressPercent);
        Assert.Equal(1, graphAfter.TrackableNodeCount);
        Assert.Equal(0, graphAfter.CompletedNodeCount);
    }

    [Fact]
    public async Task TC110_RequiredNodeCannotBeSkipped()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var nodes = fixture.AddValidGraph(version);
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.ProgressService.UpdateNodeProgressAsync(
                fixture.Learner.UserId,
                enrollment.RoadmapEnrollmentId,
                nodes.Project.RoadmapNodeId,
                new UpdateNodeProgressRequestDto { Status = "skipped" },
                CancellationToken.None));

        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await fixture.Context.UserNodeProgresses.AnyAsync());
        Assert.False(await fixture.Context.ProgressEvents.AnyAsync());
        var storedEnrollment = await fixture.Context.RoadmapEnrollments.AsNoTracking().SingleAsync();
        Assert.Equal(0m, storedEnrollment.ProgressPercent);
        Assert.Equal("active", storedEnrollment.Status);
    }

    [Fact]
    public async Task TC111_UserCannotUpdateAnotherUsersEnrollment()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var nodes = fixture.AddValidGraph(version);
        var learnerEnrollment = fixture.AddEnrollment(version, fixture.Learner);
        var otherEnrollment = fixture.AddEnrollment(version, fixture.OtherLearner);
        fixture.AddProgress(learnerEnrollment, nodes.Project, "in_progress");
        fixture.AddProgress(otherEnrollment, nodes.Project, "pending");
        await fixture.SaveAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.ProgressService.UpdateNodeProgressAsync(
                fixture.Learner.UserId,
                otherEnrollment.RoadmapEnrollmentId,
                nodes.Project.RoadmapNodeId,
                new UpdateNodeProgressRequestDto { Status = "completed" },
                CancellationToken.None));

        var storedEnrollments = await fixture.Context.RoadmapEnrollments
            .AsNoTracking()
            .OrderBy(item => item.UserId)
            .ToListAsync();
        Assert.Equal(2, storedEnrollments.Count);
        Assert.All(storedEnrollments, item =>
        {
            Assert.Equal(0m, item.ProgressPercent);
            Assert.Equal("active", item.Status);
            Assert.Null(item.CompletedAt);
        });
        var storedProgress = await fixture.Context.UserNodeProgresses
            .AsNoTracking()
            .ToDictionaryAsync(item => item.RoadmapEnrollmentId);
        Assert.Equal("in_progress", storedProgress[learnerEnrollment.RoadmapEnrollmentId].Status);
        Assert.Equal("pending", storedProgress[otherEnrollment.RoadmapEnrollmentId].Status);
        Assert.False(await fixture.Context.ProgressEvents.AnyAsync());
    }

    [Fact]
    public async Task TC112_DuplicateCompletionRequestIsIdempotent()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var nodes = fixture.AddValidGraph(version);
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();
        var request = new UpdateNodeProgressRequestDto { Status = "completed" };

        var first = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            nodes.Project.RoadmapNodeId,
            request,
            CancellationToken.None);
        var second = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            nodes.Project.RoadmapNodeId,
            request,
            CancellationToken.None);

        Assert.Equal(first.ProgressPercent, second.ProgressPercent);
        Assert.Equal(100m, second.ProgressPercent);
        Assert.Equal(1, await fixture.Context.UserNodeProgresses.CountAsync());
        var storedEnrollment = await fixture.Context.RoadmapEnrollments.AsNoTracking().SingleAsync();
        Assert.Equal(100m, storedEnrollment.ProgressPercent);
        Assert.Equal("completed", storedEnrollment.Status);
        var storedProgress = await fixture.Context.UserNodeProgresses.AsNoTracking().SingleAsync();
        Assert.Equal("completed", storedProgress.Status);
    }

    [Fact]
    public async Task TC113_EnrollmentCompletesOnlyAfterFinalRequiredNodeIsCompleted()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var firstRequired = fixture.AddNode(
            version,
            "project",
            "first-required",
            "First Required",
            phase,
            orderIndex: 1);
        var finalRequired = fixture.AddNode(
            version,
            "project",
            "final-required",
            "Final Required",
            phase,
            orderIndex: 2);
        var optional = fixture.AddNode(
            version,
            "project",
            "optional",
            "Optional",
            phase,
            isRequired: false,
            orderIndex: 3);
        fixture.AddEdge(version, firstRequired, finalRequired, "dependency", "required");
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();

        var afterFirstCompletion = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            firstRequired.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "completed" },
            CancellationToken.None);
        Assert.Equal("active", afterFirstCompletion.Enrollment.Status);
        Assert.Equal(50m, afterFirstCompletion.ProgressPercent);
        Assert.Null(afterFirstCompletion.Enrollment.CompletedAt);

        await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            optional.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "skipped" },
            CancellationToken.None);

        var result = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            finalRequired.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "completed" },
            CancellationToken.None);

        Assert.Equal("completed", result.Enrollment.Status);
        Assert.Equal(100m, result.ProgressPercent);
        Assert.Equal(2, result.TrackableNodeCount);
        Assert.Equal(2, result.CompletedNodeCount);
        Assert.NotNull(result.Enrollment.CompletedAt);
        var storedEnrollment = await fixture.Context.RoadmapEnrollments.AsNoTracking().SingleAsync();
        Assert.Equal("completed", storedEnrollment.Status);
        Assert.Equal(100m, storedEnrollment.ProgressPercent);
        Assert.NotNull(storedEnrollment.CompletedAt);
        var storedProgress = await fixture.Context.UserNodeProgresses
            .AsNoTracking()
            .ToDictionaryAsync(item => item.RoadmapNodeId);
        Assert.Equal("completed", storedProgress[firstRequired.RoadmapNodeId].Status);
        Assert.Equal("completed", storedProgress[finalRequired.RoadmapNodeId].Status);
        Assert.Equal("skipped", storedProgress[optional.RoadmapNodeId].Status);
    }

    [Fact]
    public async Task TC114_EnrollmentRemainsActiveWhileRequiredWorkIsPending()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var setup = await CreateTwoStepRoadmapAsync(fixture);

        var result = await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            setup.Enrollment.RoadmapEnrollmentId,
            setup.First.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "completed" },
            CancellationToken.None);

        Assert.Equal("active", result.Enrollment.Status);
        Assert.Equal(50m, result.ProgressPercent);
        Assert.Null(result.Enrollment.CompletedAt);
        var storedEnrollment = await fixture.Context.RoadmapEnrollments.AsNoTracking().SingleAsync();
        Assert.Equal("active", storedEnrollment.Status);
        Assert.Equal(50m, storedEnrollment.ProgressPercent);
        Assert.Null(storedEnrollment.CompletedAt);
    }

    private static async Task<(RoadmapEnrollment Enrollment, RoadmapNode First, RoadmapNode Second)> CreateTwoStepRoadmapAsync(
        RoadmapTestFixture fixture)
    {
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var first = fixture.AddNode(version, "project", "first", "First", phase, orderIndex: 1);
        var second = fixture.AddNode(version, "project", "second", "Second", phase, orderIndex: 2);
        fixture.AddEdge(version, first, second, "dependency", "required");
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();
        return (enrollment, first, second);
    }
}

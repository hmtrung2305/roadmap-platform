using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.Roadmaps;

public sealed class RoadmapCatalogEnrollmentAndMigrationTests
{
    [Fact]
    public async Task TC098_CatalogListsOnlyLatestPublishedPublicVersionPerRoadmap()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var visibleRoadmap = fixture.CreateRoadmap("Visible Roadmap");
        var published = fixture.AddVersion(visibleRoadmap, "published", 1, 0, 0);
        fixture.AddValidGraph(published);
        fixture.AddVersion(visibleRoadmap, "draft", 2, 0, 0, "major", published.RoadmapVersionId);

        var hiddenRoadmap = fixture.CreateRoadmap("Private Roadmap", visibility: "private");
        var hiddenPublished = fixture.AddVersion(hiddenRoadmap, "published", 1, 0, 0);
        fixture.AddValidGraph(hiddenPublished);
        await fixture.SaveAsync();

        var results = await fixture.QueryService.GetPublishedRoadmapsAsync(CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal(visibleRoadmap.RoadmapId, result.RoadmapId);
        Assert.Equal(published.RoadmapVersionId, result.RoadmapVersionId);
    }

    [Fact]
    public async Task TC099_CatalogReturnsCareerRoleMetadataRequiredForFiltering()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();

        var matchingRole = fixture.CareerRole;
        var otherRole = new CareerRole
        {
            CareerRoleId = Guid.NewGuid(),
            Name = "Data Analyst",
            Slug = "data-analyst",
            Description = "Analyzes data.",
            Category = "Data",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        fixture.Context.CareerRoles.Add(otherRole);

        var matchingRoadmap = fixture.CreateRoadmap(
            title: "Software Engineering Roadmap",
            careerRole: matchingRole);
        var matchingVersion = fixture.AddVersion(matchingRoadmap, "published");
        fixture.AddValidGraph(matchingVersion);

        var otherRoadmap = fixture.CreateRoadmap(
            title: "Data Analysis Roadmap",
            careerRole: otherRole);
        var otherVersion = fixture.AddVersion(otherRoadmap, "published");
        fixture.AddValidGraph(otherVersion);

        await fixture.SaveAsync();

        var result = await fixture.QueryService.GetPublishedRoadmapsAsync(
            CancellationToken.None);

        Assert.Equal(2, result.Count);

        var matchingResult = Assert.Single(result.Where(item =>
            item.CareerRole.CareerRoleId == matchingRole.CareerRoleId));

        Assert.Equal(matchingRole.Name, matchingResult.CareerRole.Name);
        Assert.Equal(matchingRole.Slug, matchingResult.CareerRole.Slug);
        Assert.Equal(matchingRole.Category, matchingResult.CareerRole.Category);

        var otherResult = Assert.Single(result.Where(item =>
            item.CareerRole.CareerRoleId == otherRole.CareerRoleId));

        Assert.Equal(otherRole.Name, otherResult.CareerRole.Name);
        Assert.Equal(otherRole.Slug, otherResult.CareerRole.Slug);
        Assert.Equal(otherRole.Category, otherResult.CareerRole.Category);
    }

    [Fact]
    public async Task TC100_DetailGraphAndLazyNodeDetailExposeThePublishedRoadmap()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var graphNodes = fixture.AddValidGraph(version);
        await fixture.SaveAsync();

        var detail = await fixture.QueryService.GetPublishedRoadmapBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        var graph = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        var nodeDetail = await fixture.QueryService.GetRoadmapNodeDetailAsync(
            version.RoadmapVersionId,
            graphNodes.Project.RoadmapNodeId,
            fixture.Learner.UserId,
            CancellationToken.None);

        Assert.Equal(version.RoadmapVersionId, detail.RoadmapVersionId);
        Assert.Equal(2, graph.Nodes.Count);
        Assert.Single(graph.Edges);
        Assert.Equal(graphNodes.Project.Title, nodeDetail.Title);
        Assert.NotEmpty(nodeDetail.LearningOutcomes);
    }

    [Fact]
    public async Task TC101_UnpublishedDraftIsHiddenFromLearnerRoutes()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddValidGraph(version);
        await fixture.SaveAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.QueryService.GetPublishedRoadmapBySlugAsync(
                roadmap.Slug,
                fixture.Learner.UserId,
                CancellationToken.None));
    }

    [Fact]
    public async Task TC102_EnrollmentCreatesOneActiveRecordAndComputedInitialProgress()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        fixture.AddValidGraph(version);
        await fixture.SaveAsync();

        var enrollment = await fixture.EnrollmentService.EnrollAsync(
            fixture.Learner.UserId,
            new EnrollRoadmapRequestDto { RoadmapVersionId = version.RoadmapVersionId },
            CancellationToken.None);
        var graph = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);

        Assert.Equal("active", enrollment.Status);
        Assert.Equal(0m, enrollment.ProgressPercent);
        Assert.Equal((Guid?)enrollment.RoadmapEnrollmentId, graph.Enrollment?.RoadmapEnrollmentId);
        Assert.Equal("pending", graph.Nodes.Single(node => node.IsTrackable).Progress.Status);
        Assert.Empty(await fixture.Context.UserNodeProgresses.ToListAsync());
    }

    [Fact]
    public async Task TC103_RepeatedEnrollmentIsIdempotent()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        fixture.AddValidGraph(version);
        await fixture.SaveAsync();
        var request = new EnrollRoadmapRequestDto { RoadmapVersionId = version.RoadmapVersionId };

        var first = await fixture.EnrollmentService.EnrollAsync(
            fixture.Learner.UserId,
            request,
            CancellationToken.None);
        var second = await fixture.EnrollmentService.EnrollAsync(
            fixture.Learner.UserId,
            request,
            CancellationToken.None);

        Assert.Equal(first.RoadmapEnrollmentId, second.RoadmapEnrollmentId);
        Assert.Equal(1, await fixture.Context.RoadmapEnrollments.CountAsync());
    }

    [Fact]
    public async Task TC104_DraftVersionCannotBeEnrolled()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var draft = fixture.AddVersion(roadmap, "draft");
        fixture.AddValidGraph(draft);
        await fixture.SaveAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.EnrollmentService.EnrollAsync(
                fixture.Learner.UserId,
                new EnrollRoadmapRequestDto { RoadmapVersionId = draft.RoadmapVersionId },
                CancellationToken.None));
    }

    [Fact]
    public async Task TC105_ProgressSummaryMatchesPersistedEnrollmentAndComputedNodes()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "published");
        var phase = fixture.AddNode(
            version,
            "phase",
            "progress-phase",
            "Progress Phase",
            isTrackable: false);
        var completedNode = fixture.AddNode(
            version,
            "project",
            "completed-project",
            "Completed Project",
            phase,
            isRequired: true,
            orderIndex: 1);
        var activeNode = fixture.AddNode(
            version,
            "project",
            "active-project",
            "Active Project",
            phase,
            isRequired: true,
            orderIndex: 2);
        var optionalNode = fixture.AddNode(
            version,
            "project",
            "optional-project",
            "Optional Project",
            phase,
            isRequired: false,
            orderIndex: 3);
        var enrollment = fixture.AddEnrollment(version);
        await fixture.SaveAsync();

        await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            completedNode.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "completed" },
            CancellationToken.None);
        await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            activeNode.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "in_progress" },
            CancellationToken.None);
        await fixture.ProgressService.UpdateNodeProgressAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            optionalNode.RoadmapNodeId,
            new UpdateNodeProgressRequestDto { Status = "skipped" },
            CancellationToken.None);

        var graph = await fixture.QueryService.GetPublishedRoadmapGraphBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        var storedEnrollment = await fixture.Context.RoadmapEnrollments
            .AsNoTracking()
            .SingleAsync(item => item.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId);
        var storedProgress = await fixture.Context.UserNodeProgresses
            .AsNoTracking()
            .Where(item => item.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId)
            .ToDictionaryAsync(item => item.RoadmapNodeId);

        Assert.Equal(2, graph.TrackableNodeCount);
        Assert.Equal(1, graph.CompletedNodeCount);
        Assert.Equal(50m, graph.ProgressPercent);
        Assert.Equal(storedEnrollment.ProgressPercent, graph.ProgressPercent);
        Assert.Equal("active", storedEnrollment.Status);

        Assert.Equal(3, storedProgress.Count);
        Assert.Equal("completed", storedProgress[completedNode.RoadmapNodeId].Status);
        Assert.Equal("in_progress", storedProgress[activeNode.RoadmapNodeId].Status);
        Assert.Equal("skipped", storedProgress[optionalNode.RoadmapNodeId].Status);

        var graphProgress = graph.Nodes
            .Where(node => node.IsTrackable)
            .ToDictionary(node => node.RoadmapNodeId, node => node.Progress.Status);
        Assert.Equal("completed", graphProgress[completedNode.RoadmapNodeId]);
        Assert.Equal("in_progress", graphProgress[activeNode.RoadmapNodeId]);
        Assert.Equal("skipped", graphProgress[optionalNode.RoadmapNodeId]);
    }

    [Fact]
    public async Task TC115_PublishedPatchAutomaticallyMigratesEnrollmentAndProgress()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 2, 3, "patch");
        var sourceNodes = fixture.AddValidGraph(source, projectSlug: "stable-project");
        var enrollment = fixture.AddEnrollment(source, progressPercent: 100, status: "completed");
        fixture.AddProgress(enrollment, sourceNodes.Project, "completed");
        await fixture.SaveAsync();

        var patch = await fixture.DraftService.CreatePatchRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            patch.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Correct wording." },
            CancellationToken.None);
        await fixture.DraftService.ApproveRoadmapVersionAsync(
            patch.RoadmapVersionId,
            fixture.Reviewer.UserId,
            CancellationToken.None);

        var storedEnrollment = await fixture.Context.RoadmapEnrollments
            .SingleAsync(item => item.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId);
        var targetNode = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == patch.RoadmapVersionId && node.Slug == sourceNodes.Project.Slug);
        var storedProgress = await fixture.Context.UserNodeProgresses
            .SingleAsync(item => item.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId);

        Assert.Equal(patch.RoadmapVersionId, storedEnrollment.RoadmapVersionId);
        Assert.Equal(targetNode.RoadmapNodeId, storedProgress.RoadmapNodeId);
        Assert.Equal("completed", storedProgress.Status);
    }

    [Fact]
    public async Task TC116_MinorUpdateIsExposedWithoutAutomaticMigration()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 2, 3, "patch");
        fixture.AddValidGraph(source, projectSlug: "stable-project");
        var target = fixture.AddVersion(
            roadmap,
            "published",
            1,
            3,
            0,
            "minor",
            source.RoadmapVersionId);
        fixture.AddValidGraph(target, projectSlug: "stable-project");
        var enrollment = fixture.AddEnrollment(source);
        await fixture.SaveAsync();

        var detail = await fixture.QueryService.GetPublishedRoadmapBySlugAsync(
            roadmap.Slug,
            fixture.Learner.UserId,
            CancellationToken.None);
        var storedEnrollment = await fixture.Context.RoadmapEnrollments
            .AsNoTracking()
            .SingleAsync(item => item.RoadmapEnrollmentId == enrollment.RoadmapEnrollmentId);

        Assert.Equal(target.RoadmapVersionId, detail.RoadmapVersionId);
        Assert.Null(detail.Enrollment);
        Assert.NotNull(detail.AvailableUpdate);
        Assert.Equal(enrollment.RoadmapEnrollmentId, detail.AvailableUpdate!.RoadmapEnrollmentId);
        Assert.Equal(source.RoadmapVersionId, detail.AvailableUpdate.CurrentRoadmapVersionId);
        Assert.Equal(target.RoadmapVersionId, detail.AvailableUpdate.TargetRoadmapVersionId);
        Assert.Equal("minor", detail.AvailableUpdate.ReleaseType);
        Assert.Equal(source.RoadmapVersionId, storedEnrollment.RoadmapVersionId);
    }

    [Fact]
    public async Task TC117_ManualMinorMigrationPreservesMatchingProgress()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 2, 3, "patch");
        var sourceNodes = fixture.AddValidGraph(source, projectSlug: "stable-project");
        var target = fixture.AddVersion(
            roadmap,
            "published",
            1,
            3,
            0,
            "minor",
            source.RoadmapVersionId);
        var targetNodes = fixture.AddValidGraph(target, projectSlug: "stable-project");
        var enrollment = fixture.AddEnrollment(source, progressPercent: 100, status: "completed");
        fixture.AddProgress(enrollment, sourceNodes.Project, "completed");
        await fixture.SaveAsync();

        var migrated = await fixture.EnrollmentService.MigrateEnrollmentAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            new MigrateRoadmapEnrollmentRequestDto { TargetRoadmapVersionId = target.RoadmapVersionId },
            CancellationToken.None);
        var progress = await fixture.Context.UserNodeProgresses.SingleAsync();

        Assert.Equal(target.RoadmapVersionId, migrated.RoadmapVersionId);
        Assert.Equal(targetNodes.Project.RoadmapNodeId, progress.RoadmapNodeId);
        Assert.Equal("completed", progress.Status);
    }

    [Fact]
    public async Task TC118_ManualMajorMigrationRequiresAnExplicitTargetAndPreservesMatchingProgress()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 2, 3, "patch");
        var sourceNodes = fixture.AddValidGraph(source, projectSlug: "stable-project");
        var target = fixture.AddVersion(
            roadmap,
            "published",
            2,
            0,
            0,
            "major",
            source.RoadmapVersionId);
        var targetNodes = fixture.AddValidGraph(target, projectSlug: "stable-project");
        var enrollment = fixture.AddEnrollment(source, progressPercent: 100, status: "completed");
        fixture.AddProgress(enrollment, sourceNodes.Project, "completed");
        await fixture.SaveAsync();

        var migrated = await fixture.EnrollmentService.MigrateEnrollmentAsync(
            fixture.Learner.UserId,
            enrollment.RoadmapEnrollmentId,
            new MigrateRoadmapEnrollmentRequestDto { TargetRoadmapVersionId = target.RoadmapVersionId },
            CancellationToken.None);
        var progress = await fixture.Context.UserNodeProgresses.SingleAsync();

        Assert.Equal(target.RoadmapVersionId, migrated.RoadmapVersionId);
        Assert.Equal(targetNodes.Project.RoadmapNodeId, progress.RoadmapNodeId);
    }

    [Fact]
    public async Task TC119_UnpublishedMigrationTargetIsRejected()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 0, 0);
        fixture.AddValidGraph(source);
        var target = fixture.AddVersion(roadmap, "draft", 2, 0, 0, "major", source.RoadmapVersionId);
        fixture.AddValidGraph(target);
        var enrollment = fixture.AddEnrollment(source);
        await fixture.SaveAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.EnrollmentService.MigrateEnrollmentAsync(
                fixture.Learner.UserId,
                enrollment.RoadmapEnrollmentId,
                new MigrateRoadmapEnrollmentRequestDto { TargetRoadmapVersionId = target.RoadmapVersionId },
                CancellationToken.None));
    }
}

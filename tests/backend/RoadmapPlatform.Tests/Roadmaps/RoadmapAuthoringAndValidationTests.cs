using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.Roadmaps;

public sealed class RoadmapAuthoringAndValidationTests
{
    [Fact]
    public async Task TC120_CreateRoadmapProducesOwnedDraftVersionOne()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();

        var result = await fixture.DraftService.CreateRoadmapAsync(
            new CreateRoadmapRequestDto
            {
                CareerRoleId = fixture.CareerRole.CareerRoleId,
                Title = "Quality Engineering Roadmap",
                Description = "A focused roadmap.",
                EstimatedTotalHours = 40,
            },
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("draft", result.Status);
        Assert.Equal("v1.0.0", result.VersionLabel);
        Assert.Equal("initial", result.ReleaseType);
        var storedRoadmap = await fixture.Context.Roadmaps.AsNoTracking().SingleAsync();
        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal(result.RoadmapId, storedRoadmap.RoadmapId);
        Assert.Equal(fixture.CareerRole.CareerRoleId, storedRoadmap.CareerRoleId);
        Assert.Equal(fixture.Owner.UserId, storedRoadmap.OwnerUserId);
        Assert.Equal(result.RoadmapVersionId, storedVersion.RoadmapVersionId);
        Assert.Equal(1, storedVersion.MajorVersion);
        Assert.Equal(0, storedVersion.MinorVersion);
        Assert.Equal(0, storedVersion.PatchVersion);
        Assert.Equal("draft", storedVersion.Status);
        var updated = await fixture.MetadataService.UpdateRoadmapVersionMetadataAsync(
            result.RoadmapVersionId,
            new UpdateRoadmapVersionMetadataRequestDto
            {
                Title = "Editable Quality Engineering Roadmap",
                Description = "Authorized author edit.",
                EstimatedTotalHours = 42,
            },
            fixture.Owner.UserId,
            CancellationToken.None);
        Assert.Equal("Editable Quality Engineering Roadmap", updated.Title);
    }

    [Fact]
    public async Task TC121_CreateRoadmapRejectsMissingTitle()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.DraftService.CreateRoadmapAsync(
                new CreateRoadmapRequestDto
                {
                    CareerRoleId = fixture.CareerRole.CareerRoleId,
                    Title = "   ",
                },
                fixture.Owner.UserId,
                CancellationToken.None));
        Assert.Empty(await fixture.Context.Roadmaps.ToListAsync());
        Assert.Empty(await fixture.Context.RoadmapVersions.ToListAsync());
    }

    [Fact]
    public async Task TC122_CreateRoadmapRejectsMissingCareerRole()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.DraftService.CreateRoadmapAsync(
                new CreateRoadmapRequestDto { Title = "Roadmap without role" },
                fixture.Owner.UserId,
                CancellationToken.None));
        Assert.Empty(await fixture.Context.Roadmaps.ToListAsync());
        Assert.Empty(await fixture.Context.RoadmapVersions.ToListAsync());
    }

    [Fact]
    public async Task TC123_DraftMetadataCanBeUpdated()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddValidGraph(version);
        var baselineUpdatedAt = DateTime.UtcNow.AddMinutes(-5);
        version.UpdatedAt = baselineUpdatedAt;
        roadmap.UpdatedAt = baselineUpdatedAt;
        await fixture.SaveAsync();

        var result = await fixture.MetadataService.UpdateRoadmapVersionMetadataAsync(
            version.RoadmapVersionId,
            new UpdateRoadmapVersionMetadataRequestDto
            {
                Title = "Updated Roadmap",
                Description = "Updated description.",
                EstimatedTotalHours = 60,
            },
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("Updated Roadmap", result.Title);
        Assert.Equal("Updated description.", result.Description);
        Assert.Equal((int?)60, result.EstimatedTotalHours);
        var stored = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal("Updated Roadmap", stored.Title);
        Assert.Equal("Updated description.", stored.Description);
        Assert.Equal(60, stored.EstimatedTotalHours);
        Assert.True(stored.UpdatedAt > baselineUpdatedAt);
        Assert.True((await fixture.Context.Roadmaps.AsNoTracking().SingleAsync()).UpdatedAt > baselineUpdatedAt);
    }

    [Fact]
    public async Task TC124_DraftSupportsAddingPhaseAndLearnerFacingNode()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        await fixture.SaveAsync();

        var phaseResult = await fixture.StructureService.CreateNodeAsync(
            version.RoadmapVersionId,
            new CreateRoadmapNodeRequestDto
            {
                NodeType = "phase",
                Title = "Core Phase",
                Position = "end",
            },
            fixture.Owner.UserId,
            CancellationToken.None);
        Assert.True(phaseResult.FocusNodeId.HasValue);
        var phaseId = phaseResult.FocusNodeId.Value;
        var projectResult = await fixture.StructureService.CreateNodeAsync(
            version.RoadmapVersionId,
            new CreateRoadmapNodeRequestDto
            {
                NodeType = "project",
                ParentNodeId = phaseId,
                Title = "Capstone Project",
                IsRequired = true,
            },
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Contains(projectResult.Roadmap.Nodes, node =>
            node.ParentNodeId == phaseId && node.NodeType == "project");
        var storedPhase = await fixture.Context.RoadmapNodes.AsNoTracking()
            .SingleAsync(node => node.RoadmapNodeId == phaseId);
        var storedProject = await fixture.Context.RoadmapNodes.AsNoTracking()
            .SingleAsync(node => node.RoadmapNodeId == projectResult.FocusNodeId);
        Assert.Equal("phase", storedPhase.NodeType);
        Assert.Equal(phaseId, storedProject.ParentNodeId);
        Assert.Equal("project", storedProject.NodeType);
        Assert.Contains(await fixture.Context.RoadmapEdges.AsNoTracking().ToListAsync(), edge =>
            edge.FromNodeId == phaseId && edge.ToNodeId == storedProject.RoadmapNodeId && edge.EdgeType == "contains");
    }

    [Fact]
    public async Task TC125_CreateNodeRejectsNonexistentParent()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        await fixture.SaveAsync();
        var nodeCountBefore = await fixture.Context.RoadmapNodes.CountAsync();
        var edgeCountBefore = await fixture.Context.RoadmapEdges.CountAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            fixture.StructureService.CreateNodeAsync(
                version.RoadmapVersionId,
                new CreateRoadmapNodeRequestDto
                {
                    NodeType = "project",
                    ParentNodeId = Guid.NewGuid(),
                    Title = "Orphan Project",
                },
                fixture.Owner.UserId,
                CancellationToken.None));
        Assert.Equal(nodeCountBefore, await fixture.Context.RoadmapNodes.CountAsync());
        Assert.Equal(edgeCountBefore, await fixture.Context.RoadmapEdges.CountAsync());
    }

    [Fact]
    public async Task TC126_ValidatorDetectsCircularParentHierarchyAndBlocksSubmission()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var group = fixture.AddNode(version, "choice_group", "group", "Group", phase, isTrackable: false);
        fixture.AddNode(version, "topic", "topic", "Topic", group);
        await fixture.SaveAsync();

        var baseline = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);
        Assert.True(baseline.IsValid);

        phase.ParentNodeId = group.RoadmapNodeId;
        fixture.AddEdge(version, group, phase, "contains");
        await fixture.SaveAsync();

        var result = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, item => item.Code == "node_parent_cycle");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
                version.RoadmapVersionId,
                fixture.Owner.UserId,
                new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Cyclic graph." },
                CancellationToken.None));
        Assert.Equal("draft", (await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task TC127_RelationalDatabaseRejectsMissingOrCrossVersionEdgeEndpoints()
    {
        await using var fixture = await RoadmapTestFixture.CreateRelationalAsync();
        var roadmap = fixture.CreateRoadmap();
        var firstVersion = fixture.AddVersion(roadmap, "draft", 1, 0, 0);
        var firstNodes = fixture.AddValidGraph(firstVersion, projectSlug: "first-project");
        var secondVersion = fixture.AddVersion(
            roadmap,
            "draft",
            2,
            0,
            0,
            "major",
            firstVersion.RoadmapVersionId);
        var secondNodes = fixture.AddValidGraph(secondVersion, projectSlug: "second-project");
        await fixture.SaveAsync();
        var edgeIdsBefore = await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Select(edge => edge.RoadmapEdgeId)
            .OrderBy(id => id)
            .ToListAsync();

        var missingEndpointEdge = new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = firstVersion.RoadmapVersionId,
            FromNodeId = Guid.NewGuid(),
            ToNodeId = firstNodes.Project.RoadmapNodeId,
            EdgeType = "dependency",
            DependencyType = "required",
            Condition = "{}",
        };
        fixture.Context.RoadmapEdges.Add(missingEndpointEdge);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
        fixture.Context.ChangeTracker.Clear();

        var crossVersionEdge = new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = firstVersion.RoadmapVersionId,
            FromNodeId = firstNodes.Phase.RoadmapNodeId,
            ToNodeId = secondNodes.Project.RoadmapNodeId,
            EdgeType = "dependency",
            DependencyType = "required",
            Condition = "{}",
        };
        fixture.Context.RoadmapEdges.Add(crossVersionEdge);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(edgeIdsBefore, await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Select(edge => edge.RoadmapEdgeId)
            .OrderBy(id => id)
            .ToListAsync());
    }

    [Fact]
    public async Task TC128_InvalidGroupRuleAndDuplicateMembershipAreRejectedWithoutMutation()
    {
        await using var fixture = await RoadmapTestFixture.CreateRelationalAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var group = fixture.AddNode(version, "choice_group", "group", "Group", phase, isTrackable: false);
        var topic = fixture.AddNode(version, "topic", "only-topic", "Only Topic", group);
        await fixture.SaveAsync();
        var edgesBefore = await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == version.RoadmapVersionId)
            .Select(edge => new
            {
                edge.RoadmapEdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.EdgeType,
                edge.DependencyType,
            })
            .OrderBy(edge => edge.RoadmapEdgeId)
            .ToListAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.StructureService.UpdateGroupRuleAsync(
                group.RoadmapNodeId,
                new UpdateRoadmapNodeGroupRuleRequestDto
                {
                    SelectionType = "choose_many",
                    RequiredCount = 2,
                },
                fixture.Owner.UserId,
                CancellationToken.None));

        var storedGroup = await fixture.Context.RoadmapNodes.AsNoTracking()
            .SingleAsync(node => node.RoadmapNodeId == group.RoadmapNodeId);
        Assert.Equal("complete_all", storedGroup.SelectionType);
        Assert.Null(storedGroup.RequiredCount);

        var duplicateMembership = new RoadmapEdge
        {
            RoadmapEdgeId = Guid.NewGuid(),
            RoadmapVersionId = version.RoadmapVersionId,
            FromNodeId = group.RoadmapNodeId,
            ToNodeId = topic.RoadmapNodeId,
            EdgeType = "choice",
            DependencyType = "required",
            Condition = "{}",
        };
        fixture.Context.RoadmapEdges.Add(duplicateMembership);
        await Assert.ThrowsAsync<DbUpdateException>(() => fixture.Context.SaveChangesAsync());
        fixture.Context.ChangeTracker.Clear();

        Assert.Equal(edgesBefore, await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == version.RoadmapVersionId)
            .Select(edge => new
            {
                edge.RoadmapEdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.EdgeType,
                edge.DependencyType,
            })
            .OrderBy(edge => edge.RoadmapEdgeId)
            .ToListAsync());
    }

    [Fact]
    public async Task TC129_ValidationFailsWhenNoPhaseExists()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddNode(
            version,
            "project",
            "orphan-project",
            "Orphan Project",
            parent: null,
            isRequired: true,
            isTrackable: true);
        await fixture.SaveAsync();

        var result = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, item => item.Code == "phase_required");
        Assert.DoesNotContain(result.Errors, item => item.Code == "learner_node_required");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
                version.RoadmapVersionId,
                fixture.Owner.UserId,
                new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Invalid submission." },
                CancellationToken.None));
        Assert.Equal("draft", (await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task TC130_ValidationFailsWhenNoLearnerFacingNodeExists()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        await fixture.SaveAsync();

        var result = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, item => item.Code == "learner_node_required");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
                version.RoadmapVersionId,
                fixture.Owner.UserId,
                new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Invalid submission." },
                CancellationToken.None));
        Assert.Equal("draft", (await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync()).Status);
    }

    [Fact]
    public async Task TC131_ValidationWarnsAboutIncompleteDescriptiveContent()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        var phase = fixture.AddNode(version, "phase", "phase", "Phase", isTrackable: false);
        var project = fixture.AddNode(
            version,
            "project",
            "project",
            "Project",
            phase,
            description: null);
        project.LearningOutcomes = "[]";
        project.CompletionCriteria = "[]";
        await fixture.SaveAsync();

        var result = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Contains(result.Warnings, item => item.Code == "node_description_missing");
        Assert.Contains(result.Warnings, item => item.Code == "learning_outcomes_missing");
        Assert.Contains(result.Warnings, item => item.Code == "completion_criteria_missing");
        Assert.Contains(result.Warnings, item => item.Code == "skills_missing");
        Assert.Contains(result.Warnings, item => item.Code == "resources_missing");
    }

    [Fact]
    public async Task TC132_ValidGraphPassesBlockingValidation()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddValidGraph(version);
        await fixture.SaveAsync();

        var result = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            version.RoadmapVersionId,
            CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        var submitted = await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Validation passed." },
            CancellationToken.None);
        Assert.Equal("pending_review", submitted.Status);
    }
}

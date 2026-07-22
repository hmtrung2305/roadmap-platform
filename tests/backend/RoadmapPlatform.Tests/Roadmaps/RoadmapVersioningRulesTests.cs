using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.Roadmaps;

public sealed class RoadmapVersioningRulesTests
{
    [Fact]
    public async Task TC141_MajorCloneCreatesVersionTwoDraft()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);

        var result = await fixture.DraftService.CloneRoadmapVersionToDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("draft", result.Status);
        Assert.Equal("major", result.ReleaseType);
        Assert.Equal("v2.0.0", result.VersionLabel);
        Assert.Equal((Guid?)source.RoadmapVersionId, result.CreatedFromVersionId);
        Assert.Equal(source.RoadmapNodes.Count, result.Nodes.Count);
    }

    [Fact]
    public async Task TC142_MinorDraftIncrementsMinorAndResetsPatch()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);

        var result = await fixture.DraftService.CreateMinorRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("v1.3.0", result.VersionLabel);
        Assert.Equal("minor", result.ReleaseType);
        Assert.Equal((Guid?)source.RoadmapVersionId, result.CreatedFromVersionId);
    }

    [Fact]
    public async Task TC143_PatchDraftIncrementsPatchOnly()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);

        var result = await fixture.DraftService.CreatePatchRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("v1.2.4", result.VersionLabel);
        Assert.Equal("patch", result.ReleaseType);
        Assert.Equal((Guid?)source.RoadmapVersionId, result.CreatedFromVersionId);
    }

    [Fact]
    public async Task TC144_PatchDraftAllowsSafeNodeContentChanges()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var patch = await fixture.DraftService.CreatePatchRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var project = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == patch.RoadmapVersionId && node.NodeType == "project");

        var result = await fixture.MetadataService.UpdateRoadmapNodeMetadataAsync(
            project.RoadmapNodeId,
            new UpdateRoadmapNodeMetadataRequestDto
            {
                Title = "Corrected Project Title",
                Description = "Corrected learner guidance.",
                EstimatedHours = 3,
                DifficultyLevel = "intermediate",
                LearningOutcomes = ["Apply the corrected concept"],
                CompletionCriteria = ["Submit the corrected project"],
            },
            fixture.Owner.UserId,
            CancellationToken.None);

        Assert.Equal("Corrected Project Title", result.Title);
        Assert.Equal("Corrected learner guidance.", result.Description);
    }

    [Fact]
    public async Task TC145_PatchDraftRejectsStructuralChanges()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var patch = await fixture.DraftService.CreatePatchRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.StructureService.CreateNodeAsync(
                patch.RoadmapVersionId,
                new CreateRoadmapNodeRequestDto
                {
                    NodeType = "phase",
                    Title = "Forbidden Phase",
                    Position = "end",
                },
                fixture.Owner.UserId,
                CancellationToken.None));

        Assert.Contains("Structural edits are not allowed", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TC146_PatchDraftRejectsSkillMappingChanges()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var skill = fixture.AddSkill("Patch Mapping Skill");
        await fixture.SaveAsync();
        var patch = await fixture.DraftService.CreatePatchRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var project = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == patch.RoadmapVersionId && node.NodeType == "project");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.MappingService.AddSkillToNodeAsync(
                project.RoadmapNodeId,
                new AddRoadmapNodeSkillRequestDto { SkillId = skill.SkillId },
                fixture.Owner.UserId,
                CancellationToken.None));
    }

    [Fact]
    public async Task TC147_MinorDraftAllowsOnlyOptionalNewLearnerNode()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var minor = await fixture.DraftService.CreateMinorRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var phase = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == minor.RoadmapVersionId && node.NodeType == "phase");

        var result = await fixture.StructureService.CreateNodeAsync(
            minor.RoadmapVersionId,
            new CreateRoadmapNodeRequestDto
            {
                NodeType = "project",
                ParentNodeId = phase.RoadmapNodeId,
                Title = "Optional Minor Project",
                IsRequired = false,
            },
            fixture.Owner.UserId,
            CancellationToken.None);
        var created = result.Roadmap.Nodes.Single(node => node.RoadmapNodeId == result.FocusNodeId);

        Assert.False(created.IsRequired);
        Assert.True(created.IsTrackable);
    }

    [Fact]
    public async Task TC148_MinorDraftRejectsRequiredNodeRemoval()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var minor = await fixture.DraftService.CreateMinorRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var project = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == minor.RoadmapVersionId && node.NodeType == "project");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.StructureService.DeleteNodeAsync(
                project.RoadmapNodeId,
                fixture.Owner.UserId,
                CancellationToken.None));
    }

    [Fact]
    public async Task TC149_MinorDraftRejectsMovingRequiredNode()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var minor = await fixture.DraftService.CreateMinorRoadmapVersionDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var project = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == minor.RoadmapVersionId && node.NodeType == "project");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.StructureService.MoveNodeAsync(
                project.RoadmapNodeId,
                new MoveRoadmapNodeRequestDto { Direction = "up" },
                fixture.Owner.UserId,
                CancellationToken.None));
    }

    [Fact]
    public async Task TC150_MajorDraftAllowsBreakingStructuralChange()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var source = await CreatePublishedSourceAsync(fixture);
        var sourceNodesBefore = await fixture.Context.RoadmapNodes
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == source.RoadmapVersionId)
            .OrderBy(node => node.RoadmapNodeId)
            .Select(node => new
            {
                node.RoadmapNodeId,
                node.ParentNodeId,
                node.Slug,
                node.NodeType,
                node.Title,
                node.OrderIndex,
                node.IsRequired,
            })
            .ToListAsync();
        var sourceEdgesBefore = await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == source.RoadmapVersionId)
            .OrderBy(edge => edge.RoadmapEdgeId)
            .Select(edge => new
            {
                edge.RoadmapEdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.EdgeType,
                edge.DependencyType,
            })
            .ToListAsync();

        var major = await fixture.DraftService.CloneRoadmapVersionToDraftAsync(
            source.RoadmapVersionId,
            new CloneRoadmapVersionDraftRequestDto(),
            fixture.Owner.UserId,
            CancellationToken.None);
        var majorPhase = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == major.RoadmapVersionId
            && node.NodeType == "phase");
        var inheritedProject = await fixture.Context.RoadmapNodes.SingleAsync(node =>
            node.RoadmapVersionId == major.RoadmapVersionId
            && node.NodeType == "project");

        await fixture.StructureService.DeleteNodeAsync(
            inheritedProject.RoadmapNodeId,
            fixture.Owner.UserId,
            CancellationToken.None);
        var createResult = await fixture.StructureService.CreateNodeAsync(
            major.RoadmapVersionId,
            new CreateRoadmapNodeRequestDto
            {
                NodeType = "project",
                ParentNodeId = majorPhase.RoadmapNodeId,
                Title = "Replacement Major Project",
                Description = "Replacement work for the breaking major release.",
                EstimatedHours = 4,
                DifficultyLevel = "intermediate",
                IsRequired = true,
                Position = "end",
            },
            fixture.Owner.UserId,
            CancellationToken.None);
        var replacementNodeId = Assert.IsType<Guid>(createResult.FocusNodeId);

        var validation = await fixture.ValidationService.ValidateRoadmapVersionAsync(
            major.RoadmapVersionId,
            CancellationToken.None);
        Assert.True(
            validation.IsValid,
            string.Join(Environment.NewLine, validation.Errors.Select(error => error.Message)));

        var submitted = await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            major.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto
            {
                ChangeLog = "Replace required project structure for the major release.",
            },
            CancellationToken.None);

        Assert.Equal("pending_review", submitted.Status);
        Assert.DoesNotContain(submitted.Nodes, node =>
            node.RoadmapNodeId == inheritedProject.RoadmapNodeId);
        Assert.Contains(submitted.Nodes, node =>
            node.RoadmapNodeId == replacementNodeId
            && node.NodeType == "project"
            && node.IsRequired);

        var sourceNodesAfter = await fixture.Context.RoadmapNodes
            .AsNoTracking()
            .Where(node => node.RoadmapVersionId == source.RoadmapVersionId)
            .OrderBy(node => node.RoadmapNodeId)
            .Select(node => new
            {
                node.RoadmapNodeId,
                node.ParentNodeId,
                node.Slug,
                node.NodeType,
                node.Title,
                node.OrderIndex,
                node.IsRequired,
            })
            .ToListAsync();
        var sourceEdgesAfter = await fixture.Context.RoadmapEdges
            .AsNoTracking()
            .Where(edge => edge.RoadmapVersionId == source.RoadmapVersionId)
            .OrderBy(edge => edge.RoadmapEdgeId)
            .Select(edge => new
            {
                edge.RoadmapEdgeId,
                edge.FromNodeId,
                edge.ToNodeId,
                edge.EdgeType,
                edge.DependencyType,
            })
            .ToListAsync();
        var sourceStatus = await fixture.Context.RoadmapVersions
            .AsNoTracking()
            .Where(version => version.RoadmapVersionId == source.RoadmapVersionId)
            .Select(version => version.Status)
            .SingleAsync();

        Assert.Equal(sourceNodesBefore, sourceNodesAfter);
        Assert.Equal(sourceEdgesBefore, sourceEdgesAfter);
        Assert.Equal("published", sourceStatus);
    }

    private static async Task<RoadmapVersion> CreatePublishedSourceAsync(RoadmapTestFixture fixture)
    {
        var roadmap = fixture.CreateRoadmap();
        var source = fixture.AddVersion(roadmap, "published", 1, 2, 3, "patch");
        fixture.AddValidGraph(source, projectSlug: "stable-project");
        await fixture.SaveAsync();
        return source;
    }
}

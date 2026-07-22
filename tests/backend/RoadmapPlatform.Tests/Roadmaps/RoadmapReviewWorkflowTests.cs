using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Controllers.Roadmaps;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Security;

namespace RoadmapPlatform.Tests.Roadmaps;

public sealed class RoadmapReviewWorkflowTests
{
    [Fact]
    public async Task TC133_ValidDraftCanBeSubmittedForReview()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreateValidDraftAsync(fixture);
        var originalTitle = version.Title;

        var result = await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Ready for review." },
            CancellationToken.None);

        Assert.Equal("pending_review", result.Status);
        Assert.Equal("submitted", result.LatestReviewEvent?.EventType);
        Assert.Equal("Ready for review.", result.LatestReviewEvent?.Message);
        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal("pending_review", storedVersion.Status);
        Assert.Single(await fixture.Context.RoadmapVersionReviewEvents.AsNoTracking().ToListAsync());

        var detailMethod = typeof(ContentManagerRoadmapsController).GetMethod(
            nameof(ContentManagerRoadmapsController.GetRoadmapDetail));
        Assert.NotNull(detailMethod);
        var accessAttribute = Assert.Single(detailMethod!.GetCustomAttributes<RequireAnyPermissionAttribute>());
        Assert.Equal(
            PermissionPolicyNames.ForAny(
                PermissionConstant.ROADMAP_DRAFT_VIEW_OWN,
                PermissionConstant.ROADMAP_REVIEW_VIEW_ANY),
            accessAttribute.Policy);
        await SeedRolesWithPermissionAsync(
            fixture,
            RoleNames.Reviewer,
            PermissionConstant.ROADMAP_REVIEW_VIEW_ANY);
        Assert.True(await HasAnyPermissionAsync(
            fixture,
            RoleNames.Reviewer,
            PermissionConstant.ROADMAP_REVIEW_VIEW_ANY));

        var reviewerView = await fixture.ContentQueryService.GetRoadmapDetailAsync(
            version.RoadmapId,
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            includeAllRoadmaps: true,
            CancellationToken.None);
        Assert.Equal("pending_review", reviewerView.Status);
        Assert.Equal(version.RoadmapVersionId, reviewerView.RoadmapVersionId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.MetadataService.UpdateRoadmapVersionMetadataAsync(
                version.RoadmapVersionId,
                new UpdateRoadmapVersionMetadataRequestDto
                {
                    Title = "Editing must be locked",
                    Description = "This should not persist.",
                },
                fixture.Owner.UserId,
                CancellationToken.None));
        var unchangedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal(originalTitle, unchangedVersion.Title);
        Assert.Equal("pending_review", unchangedVersion.Status);
    }


    [Fact]
    public async Task TC134_InvalidDraftCannotBeSubmittedForReview()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        await fixture.SaveAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
                version.RoadmapVersionId,
                fixture.Owner.UserId,
                new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Incomplete." },
                CancellationToken.None));

        Assert.Contains("phase", error.Message, StringComparison.OrdinalIgnoreCase);
        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal("draft", storedVersion.Status);
        Assert.Empty(await fixture.Context.RoadmapVersionReviewEvents.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task TC135_PendingRoadmapCanBeApprovedAndPublished()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreateValidDraftAsync(fixture);
        await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Ready." },
            CancellationToken.None);

        var result = await fixture.DraftService.ApproveRoadmapVersionAsync(
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            CancellationToken.None);

        Assert.Equal("published", result.Status);
        Assert.Equal("approved", result.LatestReviewEvent?.EventType);
        Assert.NotNull(result.PublishedAt);
        Assert.Equal(1, fixture.SkillGapConfigService.GenerateCallCount);
        var catalog = await fixture.QueryService.GetPublishedRoadmapsAsync(CancellationToken.None);
        Assert.Contains(catalog, item => item.RoadmapVersionId == version.RoadmapVersionId);
        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal("published", storedVersion.Status);
        Assert.NotNull(storedVersion.PublishedAt);
    }

    [Fact]
    public async Task TC136_ReviewerCanRequestChangesWithReasonAtMaximumLength()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreatePendingReviewAsync(fixture);
        var reason = new string('a', 4000);

        var result = await fixture.DraftService.RejectRoadmapVersionAsync(
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            new RejectRoadmapVersionReviewRequestDto { Reason = reason },
            CancellationToken.None);

        Assert.Equal("changes_requested", result.Status);
        Assert.Equal("rejected", result.LatestReviewEvent?.EventType);
        Assert.Equal(4000, result.LatestReviewEvent!.Message.Length);
        var storedEvent = await fixture.Context.RoadmapVersionReviewEvents.AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .FirstAsync();
        Assert.Equal(fixture.Reviewer.UserId, storedEvent.ActorUserId);
        Assert.Equal(reason, storedEvent.Message);
        var authorView = await fixture.ContentQueryService.GetRoadmapDetailAsync(
            version.RoadmapId,
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            includeAllRoadmaps: false,
            CancellationToken.None);
        Assert.Contains(authorView.ReviewEvents, item =>
            item.EventType == "rejected" && item.Message == reason);
    }

    [Fact]
    public async Task TC137_ReviewReasonOverMaximumLengthIsRejected()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreatePendingReviewAsync(fixture);
        var eventCountBefore = await fixture.Context.RoadmapVersionReviewEvents.CountAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.DraftService.RejectRoadmapVersionAsync(
                version.RoadmapVersionId,
                fixture.Reviewer.UserId,
                new RejectRoadmapVersionReviewRequestDto { Reason = new string('a', 4001) },
                CancellationToken.None));
        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking().SingleAsync();
        Assert.Equal("pending_review", storedVersion.Status);
        Assert.Equal(eventCountBefore, await fixture.Context.RoadmapVersionReviewEvents.CountAsync());
    }

    [Fact]
    public async Task TC138_ChangesRequestedRoadmapCanBeResubmitted()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreatePendingReviewAsync(fixture);
        await fixture.DraftService.RejectRoadmapVersionAsync(
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            new RejectRoadmapVersionReviewRequestDto { Reason = "Clarify the project criteria." },
            CancellationToken.None);
        await fixture.MetadataService.UpdateRoadmapVersionMetadataAsync(
            version.RoadmapVersionId,
            new UpdateRoadmapVersionMetadataRequestDto
            {
                Title = "Revised Roadmap",
                Description = "The requested clarification is now included.",
                EstimatedTotalHours = 24,
            },
            fixture.Owner.UserId,
            CancellationToken.None);

        var result = await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Clarified criteria." },
            CancellationToken.None);

        Assert.Equal("pending_review", result.Status);
        Assert.Equal("submitted", result.LatestReviewEvent?.EventType);
        Assert.Equal(3, result.ReviewEvents.Count);
        Assert.Equal("Revised Roadmap", result.Title);
        Assert.Contains(result.ReviewEvents, item =>
            item.EventType == "rejected" && item.Message == "Clarify the project criteria.");
    }

    [Fact]
    public async Task TC139_ApprovalEndpointRequiresPermissionThatLearnerDoesNotHave()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreatePendingReviewAsync(fixture);
        var method = typeof(ContentManagerRoadmapVersionsController).GetMethod(
            nameof(ContentManagerRoadmapVersionsController.ApproveRoadmapVersion));
        Assert.NotNull(method);
        var attribute = Assert.Single(method!.GetCustomAttributes<RequirePermissionAttribute>());
        Assert.Equal(PermissionPolicyNames.For(PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY), attribute.Policy);

        await SeedRolesWithPermissionAsync(
            fixture,
            RoleNames.Reviewer,
            PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY,
            RoleNames.Learner);

        Assert.False(await HasPermissionAsync(
            fixture,
            RoleNames.Learner,
            PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY));
        Assert.True(await HasPermissionAsync(
            fixture,
            RoleNames.Reviewer,
            PermissionConstant.ROADMAP_REVIEW_APPROVE_ANY));

        var storedVersion = await fixture.Context.RoadmapVersions.AsNoTracking()
            .SingleAsync(item => item.RoadmapVersionId == version.RoadmapVersionId);
        Assert.Equal("pending_review", storedVersion.Status);
        Assert.Equal(1, await fixture.Context.RoadmapVersionReviewEvents.CountAsync());
    }


    [Fact]
    public async Task TC140_ReviewTimelineIncludesEveryWorkflowEventInChronologicalOrder()
    {
        await using var fixture = await RoadmapTestFixture.CreateAsync();
        var version = await CreatePendingReviewAsync(fixture);
        await fixture.DraftService.RejectRoadmapVersionAsync(
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            new RejectRoadmapVersionReviewRequestDto { Reason = "Needs revision." },
            CancellationToken.None);
        await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Revision complete." },
            CancellationToken.None);
        var approved = await fixture.DraftService.ApproveRoadmapVersionAsync(
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            CancellationToken.None);
        Assert.Equal("published", approved.Status);

        var reloaded = await fixture.ContentQueryService.GetRoadmapDetailAsync(
            version.RoadmapId,
            version.RoadmapVersionId,
            fixture.Reviewer.UserId,
            includeAllRoadmaps: true,
            CancellationToken.None);
        var timeline = reloaded.ReviewEvents;

        Assert.Equal(
            new[] { "approved", "submitted", "rejected", "submitted" },
            timeline.Select(item => item.EventType));
        Assert.Equal(
            new Guid?[]
            {
                fixture.Reviewer.UserId,
                fixture.Owner.UserId,
                fixture.Reviewer.UserId,
                fixture.Owner.UserId,
            },
            timeline.Select(item => item.ActorUserId));
        Assert.Equal(
            new[]
            {
                fixture.Reviewer.Username,
                fixture.Owner.Username,
                fixture.Reviewer.Username,
                fixture.Owner.Username,
            },
            timeline.Select(item => item.ActorUsername));
        Assert.Equal("Approved for publication.", timeline[0].Message);
        Assert.Equal("Revision complete.", timeline[1].Message);
        Assert.Equal("Needs revision.", timeline[2].Message);
        Assert.Equal("Initial submission.", timeline[3].Message);
        Assert.All(timeline, item =>
        {
            Assert.NotEqual(Guid.Empty, item.RoadmapVersionReviewEventId);
            Assert.NotEqual(default, item.CreatedAt);
            Assert.False(string.IsNullOrWhiteSpace(item.ActorDisplayName));
        });
        Assert.All(timeline.Zip(timeline.Skip(1)), pair =>
            Assert.True(pair.First.CreatedAt >= pair.Second.CreatedAt));
        Assert.Equal(4, await fixture.Context.RoadmapVersionReviewEvents.CountAsync());
    }


    private static async Task SeedRolesWithPermissionAsync(
        RoadmapTestFixture fixture,
        string permittedRoleName,
        string permissionName,
        params string[] unpermittedRoleNames)
    {
        var permittedRole = new Role
        {
            RoleId = Guid.NewGuid(),
            RoleName = permittedRoleName,
        };
        var permission = new Permission
        {
            PermissionId = Guid.NewGuid(),
            PermissionName = permissionName,
        };
        var mapping = new PermissionRole
        {
            Id = Guid.NewGuid(),
            RoleId = permittedRole.RoleId,
            Role = permittedRole,
            PermissionId = permission.PermissionId,
            Permission = permission,
        };
        permittedRole.PermissionRoles.Add(mapping);
        permission.PermissionRoles.Add(mapping);
        fixture.Context.AddRange(permittedRole, permission, mapping);

        foreach (var roleName in unpermittedRoleNames)
        {
            fixture.Context.Roles.Add(new Role
            {
                RoleId = Guid.NewGuid(),
                RoleName = roleName,
            });
        }

        await fixture.Context.SaveChangesAsync();
    }

    private static async Task<bool> HasPermissionAsync(
        RoadmapTestFixture fixture,
        string roleName,
        string permissionName)
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var handler = new PermissionHandler(new PermissionCache(memoryCache, fixture.Context));
        var requirement = new PermissionRequirement(permissionName);
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(roleName),
            resource: null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static async Task<bool> HasAnyPermissionAsync(
        RoadmapTestFixture fixture,
        string roleName,
        params string[] permissionNames)
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var handler = new AnyPermissionHandler(new PermissionCache(memoryCache, fixture.Context));
        var requirement = new AnyPermissionRequirement(permissionNames);
        var context = new AuthorizationHandlerContext(
            [requirement],
            CreatePrincipal(roleName),
            resource: null);

        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }

    private static async Task<RoadmapPlatform.Infrastructure.Entities.RoadmapVersion> CreateValidDraftAsync(
        RoadmapTestFixture fixture)
    {
        var roadmap = fixture.CreateRoadmap();
        var version = fixture.AddVersion(roadmap, "draft");
        fixture.AddValidGraph(version);
        await fixture.SaveAsync();
        return version;
    }

    private static async Task<RoadmapPlatform.Infrastructure.Entities.RoadmapVersion> CreatePendingReviewAsync(
        RoadmapTestFixture fixture)
    {
        var version = await CreateValidDraftAsync(fixture);
        await fixture.DraftService.SubmitRoadmapVersionForReviewAsync(
            version.RoadmapVersionId,
            fixture.Owner.UserId,
            new SubmitRoadmapVersionReviewRequestDto { ChangeLog = "Initial submission." },
            CancellationToken.None);
        return version;
    }

    private static ClaimsPrincipal CreatePrincipal(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, role)],
            authenticationType: "test"));
    }

}

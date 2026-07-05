using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.SkillGapAnalysis;

namespace RoadmapPlatform.Tests;

public sealed class SkillGapVersionScopeTests
{
    [Fact]
    public async Task AssessmentUsesCategoryConfigurationForPublishedVersionOnly()
    {
        await using var context = CreateContext();
        var roadmapId = Guid.NewGuid();
        var publishedVersionId = Guid.NewGuid();
        var draftVersionId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var owner = new User
        {
            UserId = Guid.NewGuid(),
            Username = "author",
            UsernameNormalized = "AUTHOR",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now,
            UserProfile = new UserProfile
            {
                DisplayName = "Roadmap Author",
                CreatedAt = now,
                UpdatedAt = now,
            },
        };

        owner.UserProfile.UserId = owner.UserId;
        owner.UserProfile.User = owner;

        var careerRole = new CareerRole
        {
            CareerRoleId = Guid.NewGuid(),
            Name = "Frontend Engineer",
            Slug = "frontend-engineer",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var roadmap = new Roadmap
        {
            RoadmapId = roadmapId,
            CareerRoleId = careerRole.CareerRoleId,
            CareerRole = careerRole,
            OwnerUserId = owner.UserId,
            OwnerUser = owner,
            Title = "Frontend Roadmap",
            Slug = "frontend-roadmap",
            Visibility = "public",
            CreatedAt = now,
            UpdatedAt = now,
        };

        var publishedVersion = CreateVersion(
            roadmapId,
            publishedVersionId,
            roadmap,
            "published",
            versionNumber: 2,
            title: "Published");
        var draftVersion = CreateVersion(
            roadmapId,
            draftVersionId,
            roadmap,
            "draft",
            versionNumber: 3,
            title: "Draft");

        roadmap.RoadmapVersions.Add(publishedVersion);
        roadmap.RoadmapVersions.Add(draftVersion);

        var currentSkill = new Skill
        {
            SkillId = Guid.NewGuid(),
            Name = "React",
            Slug = "react",
            Category = "Current Frontend",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var currentNode = CreateNode(publishedVersionId, publishedVersion, "react-node");
        var currentNodeSkill = new RoadmapNodeSkill
        {
            RoadmapNodeSkillId = Guid.NewGuid(),
            RoadmapNodeId = currentNode.RoadmapNodeId,
            RoadmapNode = currentNode,
            SkillId = currentSkill.SkillId,
            Skill = currentSkill,
        };

        currentNode.RoadmapNodeSkills.Add(currentNodeSkill);
        currentSkill.RoadmapNodeSkills.Add(currentNodeSkill);
        publishedVersion.RoadmapNodes.Add(currentNode);

        context.AddRange(
            owner,
            careerRole,
            roadmap,
            publishedVersion,
            draftVersion,
            currentSkill,
            currentNode,
            currentNodeSkill,
            CreateCategoryConfig(
                roadmapId,
                publishedVersionId,
                roadmap,
                publishedVersion,
                "Current Frontend",
                displayOrder: 1),
            CreateCategoryConfig(
                roadmapId,
                draftVersionId,
                roadmap,
                draftVersion,
                "Draft Only",
                displayOrder: 2));

        await context.SaveChangesAsync();

        var service = new SkillGapAssessmentService(context);

        var assessment = await service.GetAssessmentAsync(roadmapId);

        var category = Assert.Single(assessment.Categories);
        Assert.Equal("Current Frontend", category.CategoryName);
        Assert.Equal(1, category.DisplayOrder);

        var skill = Assert.Single(category.Skills);
        Assert.Equal(currentSkill.SkillId, skill.SkillId);
        Assert.Equal("React", skill.SkillName);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new TestApplicationDbContext(options);
    }

    private sealed class TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
        }
    }

    private static RoadmapVersion CreateVersion(
        Guid roadmapId,
        Guid roadmapVersionId,
        Roadmap roadmap,
        string status,
        int versionNumber,
        string title)
    {
        var now = DateTime.UtcNow;

        return new RoadmapVersion
        {
            RoadmapVersionId = roadmapVersionId,
            RoadmapId = roadmapId,
            Roadmap = roadmap,
            VersionNumber = versionNumber,
            MajorVersion = versionNumber,
            MinorVersion = 0,
            PatchVersion = 0,
            ReleaseType = "minor",
            Status = status,
            Title = title,
            LayoutDirection = "TB",
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = status == "published" ? now : null,
        };
    }

    private static RoadmapNode CreateNode(
        Guid roadmapVersionId,
        RoadmapVersion roadmapVersion,
        string slug)
    {
        return new RoadmapNode
        {
            RoadmapNodeId = Guid.NewGuid(),
            RoadmapVersionId = roadmapVersionId,
            RoadmapVersion = roadmapVersion,
            Slug = slug,
            NodeType = "topic",
            Title = "React",
            OrderIndex = 1,
            LayoutRole = "skill",
            Metadata = "{}",
            IsRequired = true,
            IsTrackable = true,
            LearningOutcomes = "[]",
            CompletionCriteria = "[]",
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static SkillGapCategoryConfig CreateCategoryConfig(
        Guid roadmapId,
        Guid roadmapVersionId,
        Roadmap roadmap,
        RoadmapVersion roadmapVersion,
        string categoryName,
        int displayOrder)
    {
        var now = DateTime.UtcNow;

        return new SkillGapCategoryConfig
        {
            SkillGapCategoryConfigId = Guid.NewGuid(),
            RoadmapId = roadmapId,
            Roadmap = roadmap,
            RoadmapVersionId = roadmapVersionId,
            RoadmapVersion = roadmapVersion,
            CategoryName = categoryName,
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }
}

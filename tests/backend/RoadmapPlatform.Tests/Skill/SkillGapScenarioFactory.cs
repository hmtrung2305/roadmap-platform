using System.Text.Json;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests;

internal static class SkillGapScenarioFactory
{
    public static async Task<SkillGapScenario> CreateAsync(
        ApplicationDbContext db,
        bool published = true,
        bool includeCategoryConfiguration = true)
    {
        var now = DateTime.UtcNow;
        var owner = TestEntityFactory.CreateUser("roadmap-author");
        var learner = TestEntityFactory.CreateUser("skill-gap-learner");
        var otherLearner = TestEntityFactory.CreateUser("other-learner");
        var profile = new UserProfile
        {
            UserId = owner.UserId,
            User = owner,
            DisplayName = "Roadmap Author",
            IsPublic = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        owner.UserProfile = profile;

        var careerRole = new CareerRole
        {
            CareerRoleId = Guid.NewGuid(),
            Name = "Backend Engineer",
            Slug = "backend-engineer",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var roadmap = new Roadmap
        {
            RoadmapId = Guid.NewGuid(),
            CareerRoleId = careerRole.CareerRoleId,
            CareerRole = careerRole,
            OwnerUserId = owner.UserId,
            OwnerUser = owner,
            Title = "Backend Engineering Roadmap",
            Slug = "backend-engineering-roadmap",
            Visibility = "public",
            CreatedAt = now,
            UpdatedAt = now,
        };
        careerRole.Roadmaps.Add(roadmap);
        owner.Roadmaps.Add(roadmap);

        var version = new RoadmapVersion
        {
            RoadmapVersionId = Guid.NewGuid(),
            RoadmapId = roadmap.RoadmapId,
            Roadmap = roadmap,
            VersionNumber = 1,
            MajorVersion = 1,
            MinorVersion = 0,
            PatchVersion = 0,
            ReleaseType = "major",
            Status = published ? "published" : "draft",
            Title = published ? "Version 1.0" : "Draft Version",
            LayoutDirection = "TB",
            CreatedAt = now,
            UpdatedAt = now,
            PublishedAt = published ? now : null,
        };
        roadmap.RoadmapVersions.Add(version);

        var testingSkill = TestEntityFactory.CreateSkill("xUnit", "xunit", "Testing");
        var backendSkill = TestEntityFactory.CreateSkill("ASP.NET Core", "asp-net-core", "Backend");
        var frontendSkill = TestEntityFactory.CreateSkill("React", "react", "Frontend");

        AddSkillNode(version, testingSkill, 1);
        AddSkillNode(version, backendSkill, 2);
        AddSkillNode(version, frontendSkill, 3);

        if (includeCategoryConfiguration && published)
        {
            AddCategoryConfig(roadmap, version, "Testing", 1, now);
            AddCategoryConfig(roadmap, version, "Backend", 2, now);
            AddCategoryConfig(roadmap, version, "Frontend", 3, now);
        }

        db.AddRange(owner, learner, otherLearner, careerRole, roadmap, testingSkill, backendSkill, frontendSkill);
        await db.SaveChangesAsync();

        return new SkillGapScenario(
            learner.UserId,
            otherLearner.UserId,
            owner.UserId,
            careerRole.CareerRoleId,
            roadmap.RoadmapId,
            version.RoadmapVersionId,
            testingSkill.SkillId,
            backendSkill.SkillId,
            frontendSkill.SkillId,
            roadmap.Slug);
    }

    public static SkillGapAnalysisHistory CreateHistory(
        SkillGapScenario scenario,
        Guid userId,
        DateTime createdAt,
        Guid? historyId = null,
        AnalyzeSkillGapResponseDto? snapshot = null)
    {
        var id = historyId ?? Guid.NewGuid();
        var response = snapshot ?? new AnalyzeSkillGapResponseDto
        {
            SkillGapAnalysisHistoryId = id,
            RoadmapId = scenario.RoadmapId,
            RoadmapVersionId = scenario.RoadmapVersionId,
            RoadmapSlug = scenario.RoadmapSlug,
            RoadmapName = "Backend Engineering Roadmap",
            CareerRoleName = "Backend Engineer",
            RoadmapVersionTitle = "Version 1.0",
            RoadmapVersionNumber = 1,
            AuthorName = "Roadmap Author",
            MatchedSkills = 1,
            TotalSkills = 3,
            MissingSkills = 2,
            Categories = [],
        };

        return new SkillGapAnalysisHistory
        {
            SkillGapAnalysisHistoryId = id,
            UserId = userId,
            CareerRoleId = scenario.CareerRoleId,
            RoadmapId = scenario.RoadmapId,
            RoadmapVersionId = scenario.RoadmapVersionId,
            CareerRoleNameSnapshot = "Backend Engineer",
            RoadmapTitleSnapshot = "Backend Engineering Roadmap",
            RoadmapVersionTitleSnapshot = "Version 1.0",
            AuthorNameSnapshot = "Roadmap Author",
            MatchedSkills = response.MatchedSkills,
            TotalSkills = response.TotalSkills,
            MissingSkills = response.MissingSkills,
            SnapshotJson = JsonSerializer.Serialize(response),
            CreatedAt = createdAt,
        };
    }

    private static void AddSkillNode(RoadmapVersion version, Skill skill, int order)
    {
        var node = new RoadmapNode
        {
            RoadmapNodeId = Guid.NewGuid(),
            RoadmapVersionId = version.RoadmapVersionId,
            RoadmapVersion = version,
            Slug = $"node-{order}",
            NodeType = "topic",
            Title = skill.Name,
            OrderIndex = order,
            LayoutRole = "skill",
            Metadata = "{}",
            IsRequired = true,
            IsTrackable = true,
            LearningOutcomes = "[]",
            CompletionCriteria = "[]",
            CreatedAt = DateTime.UtcNow,
        };
        var mapping = new RoadmapNodeSkill
        {
            RoadmapNodeSkillId = Guid.NewGuid(),
            RoadmapNodeId = node.RoadmapNodeId,
            RoadmapNode = node,
            SkillId = skill.SkillId,
            Skill = skill,
        };
        node.RoadmapNodeSkills.Add(mapping);
        skill.RoadmapNodeSkills.Add(mapping);
        version.RoadmapNodes.Add(node);
    }

    private static void AddCategoryConfig(
        Roadmap roadmap,
        RoadmapVersion version,
        string name,
        int order,
        DateTime now)
    {
        var config = new SkillGapCategoryConfig
        {
            SkillGapCategoryConfigId = Guid.NewGuid(),
            RoadmapId = roadmap.RoadmapId,
            Roadmap = roadmap,
            RoadmapVersionId = version.RoadmapVersionId,
            RoadmapVersion = version,
            CategoryName = name,
            DisplayOrder = order,
            CreatedAt = now,
            UpdatedAt = now,
        };
        roadmap.SkillGapCategoryConfigs.Add(config);
        version.SkillGapCategoryConfigs.Add(config);
    }
}

internal sealed record SkillGapScenario(
    Guid LearnerUserId,
    Guid OtherLearnerUserId,
    Guid OwnerUserId,
    Guid CareerRoleId,
    Guid RoadmapId,
    Guid RoadmapVersionId,
    Guid TestingSkillId,
    Guid BackendSkillId,
    Guid FrontendSkillId,
    string RoadmapSlug);

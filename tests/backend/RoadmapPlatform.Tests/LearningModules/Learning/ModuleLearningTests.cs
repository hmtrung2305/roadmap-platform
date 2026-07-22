using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Learning;

public sealed class ModuleLearningTests
{
    [Fact]
    [Trait("TestCaseId", "TC204")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Learning")]
    [Trait("TestType", "Integration")]
    public async Task TC204_PublicCatalog_ShouldShowPublishedAndHideDraftAndArchivedModules()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var published = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published, "published");
        var draft = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft, "draft");
        var archived = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Archived, "archived");
        context.Skills.Add(skill);
        context.SkillModules.AddRange(published, draft, archived);
        await context.SaveChangesAsync();

        var result = await new LearnerLearningModuleService(context, new FakeFileStorage())
            .GetPublishedModulesAsync(CancellationToken.None);

        var module = Assert.Single(result);
        Assert.Equal(published.SkillModuleId, module.SkillModuleId);
        Assert.Equal(LearningModuleStatusValues.Published, module.Status);
    }

    [Fact]
    [Trait("TestCaseId", "TC205")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Learning")]
    [Trait("TestType", "Integration")]
    public async Task TC205_SkillFilter_ShouldReturnOnlyPublishedModulesMappedToSelectedSkill()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var csharp = Tester4TestSupport.CreateSkill(slug: "csharp");
        var react = Tester4TestSupport.CreateSkill(slug: "react");
        csharp.Name = "C#";
        react.Name = "React";
        var csharpPublished = Tester4TestSupport.CreateModule(ownerId, csharp.SkillId, LearningModuleStatusValues.Published, "csharp-published");
        var csharpDraft = Tester4TestSupport.CreateModule(ownerId, csharp.SkillId, LearningModuleStatusValues.Draft, "csharp-draft");
        var reactPublished = Tester4TestSupport.CreateModule(ownerId, react.SkillId, LearningModuleStatusValues.Published, "react-published");
        context.Skills.AddRange(csharp, react);
        context.SkillModules.AddRange(csharpPublished, csharpDraft, reactPublished);
        await context.SaveChangesAsync();

        var result = await new LearnerLearningModuleService(context, new FakeFileStorage())
            .GetPublishedModulesBySkillSlugAsync("csharp", null, CancellationToken.None);

        var module = Assert.Single(result.Modules);
        Assert.Equal(csharpPublished.SkillModuleId, module.SkillModuleId);
        Assert.Equal(csharp.SkillId, module.SkillId);
    }

    [Fact]
    [Trait("TestCaseId", "TC206")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Learning")]
    [Trait("TestType", "Integration")]
    public async Task TC206_EnrolledOnly_ShouldReturnOnlyLearnerEnrollments()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var otherLearnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var enrolledPublished = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published, "enrolled-published");
        var notEnrolled = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published, "not-enrolled");
        var enrolledArchived = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Archived, "enrolled-archived");
        context.Skills.Add(skill);
        context.SkillModules.AddRange(enrolledPublished, notEnrolled, enrolledArchived);
        context.SkillModuleEnrollments.AddRange(
            CreateEnrollment(learnerId, enrolledPublished.SkillModuleId),
            CreateEnrollment(learnerId, enrolledArchived.SkillModuleId),
            CreateEnrollment(otherLearnerId, notEnrolled.SkillModuleId));
        await context.SaveChangesAsync();

        var result = await new LearnerLearningModuleService(context, new FakeFileStorage())
            .GetEnrolledModulesAsync(learnerId, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, item => item.SkillModuleId == enrolledPublished.SkillModuleId);
        Assert.Contains(result, item => item.SkillModuleId == enrolledArchived.SkillModuleId);
        Assert.DoesNotContain(result, item => item.SkillModuleId == notEnrolled.SkillModuleId);
    }

    [Fact]
    [Trait("TestCaseId", "TC211")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Learning")]
    [Trait("TestType", "Integration")]
    public async Task TC211_ExistingLearner_ShouldAccessArchivedModuleAndLesson()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Archived, "archived-module");
        var lesson = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1, "archived/lesson.md");
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleLessons.Add(lesson);
        context.SkillModuleEnrollments.Add(CreateEnrollment(learnerId, module.SkillModuleId));
        storage.Seed("archived/lesson.md", "# Preserved archived lesson");
        await context.SaveChangesAsync();

        var service = new LearnerLearningModuleService(context, storage);
        var overview = await service.GetPublishedModuleBySlugAsync(module.Slug, learnerId, CancellationToken.None);
        var content = await service.GetLessonContentAsync(
            learnerId,
            module.SkillModuleId,
            lesson.SkillModuleLessonId,
            CancellationToken.None);

        Assert.Equal(LearningModuleStatusValues.Archived, overview.Status);
        Assert.NotNull(overview.Enrollment);
        Assert.Contains("Preserved archived lesson", content.Markdown, StringComparison.Ordinal);
    }

    private static SkillModuleEnrollment CreateEnrollment(Guid userId, Guid moduleId)
    {
        var now = DateTime.UtcNow;
        return new SkillModuleEnrollment
        {
            SkillModuleEnrollmentId = Guid.NewGuid(),
            UserId = userId,
            SkillModuleId = moduleId,
            Status = LearningModuleEnrollmentStatusValues.InProgress,
            StartedAt = now,
            ProgressPercent = 0,
            LessonProgress = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

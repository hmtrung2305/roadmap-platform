using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Publishing;

public sealed class ModulePublishingTests
{
    [Fact]
    [Trait("TestCaseId", "TC195")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Unit")]
    public void TC195_ModuleWithFewerThanThreeLessons_ShouldNotPublish()
    {
        var module = CreateReadyModuleGraph(lessonCount: 2, questionCount: 10);

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Errors, error => error.Contains("at least 3 lessons", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("TestCaseId", "TC196")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Unit")]
    public void TC196_ModuleWithMissingLessonContent_ShouldNotPublish()
    {
        var module = CreateReadyModuleGraph(lessonCount: 3, questionCount: 10);
        module.SkillModuleLessons.First().MarkdownFileKey = string.Empty;

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Errors, error => error.Contains("content to every lesson", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("TestCaseId", "TC197")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Unit")]
    public void TC197_ModuleWithIncompleteLessonIndexing_ShouldNotPublish()
    {
        var module = CreateReadyModuleGraph(lessonCount: 3, questionCount: 10);
        var staleLesson = module.SkillModuleLessons.First();
        staleLesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Pending;
        staleLesson.SkillModuleChunks.Clear();

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Checks, check => check.Key == "lesson_indexing" && !check.IsComplete);
    }

    [Fact]
    [Trait("TestCaseId", "TC198")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Unit")]
    public void TC198_ModuleWithFewerThanTenQuestions_ShouldNotPublish()
    {
        var module = CreateReadyModuleGraph(lessonCount: 3, questionCount: 9);

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Errors, error => error.Contains("at least 10 quiz questions", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    [Trait("TestCaseId", "TC199")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Integration")]
    public async Task TC199_ReadyModule_ShouldPublishAndAppearInLearnerCatalog()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var skill = Tester4TestSupport.CreateSkill();
        var module = CreateReadyModuleGraph(3, 10, ownerId, skill.SkillId);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        var result = await new ContentManagerLearningModuleService(context, storage).PublishModuleAsync(
            ownerId,
            module.SkillModuleId,
            CancellationToken.None);
        var catalog = await new LearnerLearningModuleService(context, storage)
            .GetPublishedModulesAsync(CancellationToken.None);

        Assert.Equal(LearningModuleStatusValues.Published, result.Status);
        Assert.True(
            result.PublishedAt > DateTimeOffset.MinValue,
            "PublishedAt should be assigned when the module is published.");
        Assert.Contains(catalog, item => item.SkillModuleId == module.SkillModuleId);
    }

    [Fact]
    [Trait("TestCaseId", "TC200")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Integration")]
    public async Task TC200_AlreadyPublishedModule_ShouldNotCreateDuplicatePublication()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = CreateReadyModuleGraph(3, 10, ownerId, skill.SkillId);
        module.Status = LearningModuleStatusValues.Published;
        module.PublishedAt = DateTime.UtcNow;
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();
        var publishedAt = module.PublishedAt;

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ContentManagerLearningModuleService(context, new FakeFileStorage()).PublishModuleAsync(
                ownerId,
                module.SkillModuleId,
                CancellationToken.None));

        Assert.Equal(1, await context.SkillModules.CountAsync(item => item.SkillModuleId == module.SkillModuleId));
        Assert.Equal(publishedAt, module.PublishedAt);
    }

    [Fact]
    [Trait("TestCaseId", "TC201")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Integration")]
    public async Task TC201_ArchivePublishedModule_ShouldHideNewEnrollmentButKeepExistingLearnerAccess()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleEnrollments.Add(CreateEnrollment(learnerId, module.SkillModuleId));
        await context.SaveChangesAsync();

        var authoringService = new ContentManagerLearningModuleService(context, storage);
        await authoringService.ArchiveModuleAsync(ownerId, module.SkillModuleId, CancellationToken.None);

        var learnerService = new LearnerLearningModuleService(context, storage);
        var publicCatalog = await learnerService.GetPublishedModulesAsync(CancellationToken.None);
        var enrolled = await learnerService.GetEnrolledModulesAsync(learnerId, CancellationToken.None);

        Assert.DoesNotContain(publicCatalog, item => item.SkillModuleId == module.SkillModuleId);
        Assert.Contains(enrolled, item => item.SkillModuleId == module.SkillModuleId && item.Status == LearningModuleStatusValues.Archived);
        await Assert.ThrowsAsync<NotFoundException>(() => learnerService.EnrollAsync(
            Guid.NewGuid(),
            module.SkillModuleId,
            CancellationToken.None));
    }

    [Fact]
    [Trait("TestCaseId", "TC202")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Publishing")]
    [Trait("TestType", "Integration")]
    public async Task TC202_DraftModule_ShouldNotBeArchived()
    {
        var ownerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<ConflictException>(() =>
            new ContentManagerLearningModuleService(context, new FakeFileStorage()).ArchiveModuleAsync(
                ownerId,
                module.SkillModuleId,
                CancellationToken.None));

        Assert.Equal(LearningModuleStatusValues.Draft, module.Status);
    }

    private static PublishLearningModuleReadinessDto Validate(SkillModule module)
    {
        return Tester4TestSupport.InvokePrivateStatic<PublishLearningModuleReadinessDto>(
            typeof(ContentManagerLearningModuleService),
            "ValidatePublishReadiness",
            module);
    }

    private static SkillModule CreateReadyModuleGraph(
        int lessonCount,
        int questionCount,
        Guid? ownerId = null,
        Guid? skillId = null)
    {
        var module = Tester4TestSupport.CreateModule(
            ownerId ?? Guid.NewGuid(),
            skillId ?? Guid.NewGuid(),
            LearningModuleStatusValues.Draft,
            $"module-{Guid.NewGuid():N}");

        for (var index = 1; index <= lessonCount; index++)
        {
            var lesson = Tester4TestSupport.CreateLesson(module.SkillModuleId, index);
            Tester4TestSupport.AddIndexedChunk(lesson);
            module.SkillModuleLessons.Add(lesson);
        }

        module.SkillModuleQuiz = Tester4TestSupport.CreateQuiz(module.SkillModuleId, questionCount);
        return module;
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

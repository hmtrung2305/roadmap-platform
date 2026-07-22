using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Enrollment;

public sealed class ModuleEnrollmentTests
{
    [Fact]
    [Trait("TestCaseId", "TC207")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Enrollment")]
    [Trait("TestType", "Integration")]
    public async Task TC207_EnrollPublishedModule_ShouldCreateInitialEnrollmentAndAllowFirstLesson()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var storage = new FakeFileStorage();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        var lesson = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1, "published/first.md");
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleLessons.Add(lesson);
        storage.Seed("published/first.md", "# First lesson");
        await context.SaveChangesAsync();

        var service = new LearnerLearningModuleService(context, storage);
        var enrollment = await service.EnrollAsync(learnerId, module.SkillModuleId, CancellationToken.None);
        var content = await service.GetLessonContentAsync(
            learnerId,
            module.SkillModuleId,
            lesson.SkillModuleLessonId,
            CancellationToken.None);

        Assert.Equal(LearningModuleEnrollmentStatusValues.InProgress, enrollment.Status);
        Assert.Equal(0m, enrollment.ProgressPercent);
        Assert.Empty(enrollment.LessonProgress);
        Assert.Equal(lesson.SkillModuleLessonId, content.SkillModuleLessonId);
        Assert.Single(await context.SkillModuleEnrollments.ToListAsync());
    }

    [Fact]
    [Trait("TestCaseId", "TC208")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Enrollment")]
    [Trait("TestType", "Integration")]
    public async Task TC208_RepeatedEnrollment_ShouldBeIdempotentAndPreserveProgress()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        var service = new LearnerLearningModuleService(context, new FakeFileStorage());
        var first = await service.EnrollAsync(learnerId, module.SkillModuleId, CancellationToken.None);
        var saved = await context.SkillModuleEnrollments.SingleAsync();
        saved.ProgressPercent = 40;
        saved.LessonProgress = "{\"11111111-1111-1111-1111-111111111111\":\"completed\"}";
        await context.SaveChangesAsync();

        var second = await service.EnrollAsync(learnerId, module.SkillModuleId, CancellationToken.None);

        Assert.Equal(first.SkillModuleEnrollmentId, second.SkillModuleEnrollmentId);
        Assert.Equal(40m, second.ProgressPercent);
        Assert.Equal(1, await context.SkillModuleEnrollments.CountAsync());
    }

    [Fact]
    [Trait("TestCaseId", "TC209")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Enrollment")]
    [Trait("TestType", "Integration")]
    public async Task TC209_DraftModuleEnrollment_ShouldBeRejectedWithoutExposure()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Draft);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new LearnerLearningModuleService(context, new FakeFileStorage()).EnrollAsync(
                learnerId,
                module.SkillModuleId,
                CancellationToken.None));

        Assert.Empty(context.SkillModuleEnrollments);
    }

    [Fact]
    [Trait("TestCaseId", "TC210")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Module Enrollment")]
    [Trait("TestType", "Integration")]
    public async Task TC210_NewArchivedModuleEnrollment_ShouldBeRejected()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Archived);
        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            new LearnerLearningModuleService(context, new FakeFileStorage()).EnrollAsync(
                learnerId,
                module.SkillModuleId,
                CancellationToken.None));

        Assert.Empty(context.SkillModuleEnrollments);
    }
}

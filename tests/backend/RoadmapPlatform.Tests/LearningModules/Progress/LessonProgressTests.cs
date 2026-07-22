using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.LearningModules.Progress;

public sealed class LessonProgressTests
{
    [Fact]
    [Trait("TestCaseId", "TC212")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Progress")]
    [Trait("TestType", "Integration")]
    public async Task TC212_InProgressLesson_ShouldPersistStatusWithoutIncreasingCompletedProgress()
    {
        var fixture = await CreateFixtureAsync();
        await using var context = fixture.Context;
        var service = new LearnerLearningModuleService(context, new FakeFileStorage());

        var result = await service.UpdateLessonProgressAsync(
            fixture.LearnerId,
            fixture.Module.SkillModuleId,
            fixture.Lessons[0].SkillModuleLessonId,
            new UpdateLessonProgressRequestDto
            {
                Status = LearningModuleLessonProgressStatusValues.InProgress
            },
            CancellationToken.None);

        Assert.Equal(LearningModuleLessonProgressStatusValues.InProgress, result.LessonStatus);
        Assert.Equal(0m, result.ProgressPercent);
        Assert.Contains(fixture.Lessons[0].SkillModuleLessonId.ToString(), fixture.Enrollment.LessonProgress, StringComparison.Ordinal);
        Assert.Contains(LearningModuleLessonProgressStatusValues.InProgress, fixture.Enrollment.LessonProgress, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("TestCaseId", "TC213")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Progress")]
    [Trait("TestType", "Integration")]
    public async Task TC213_CompleteLesson_ShouldIncreaseOneUnitOnlyAndRemainIdempotent()
    {
        var fixture = await CreateFixtureAsync();
        await using var context = fixture.Context;
        var service = new LearnerLearningModuleService(context, new FakeFileStorage());
        var request = new UpdateLessonProgressRequestDto
        {
            Status = LearningModuleLessonProgressStatusValues.Completed
        };

        var first = await service.UpdateLessonProgressAsync(
            fixture.LearnerId,
            fixture.Module.SkillModuleId,
            fixture.Lessons[0].SkillModuleLessonId,
            request,
            CancellationToken.None);
        var second = await service.UpdateLessonProgressAsync(
            fixture.LearnerId,
            fixture.Module.SkillModuleId,
            fixture.Lessons[0].SkillModuleLessonId,
            request,
            CancellationToken.None);

        // Two lessons plus one quiz = three total units; one completed lesson = 33.33%.
        Assert.Equal(33.33m, first.ProgressPercent);
        Assert.Equal(first.ProgressPercent, second.ProgressPercent);
        Assert.Equal(LearningModuleLessonProgressStatusValues.Completed, second.LessonStatus);
    }

    [Fact]
    [Trait("TestCaseId", "TC214")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "Lesson Progress")]
    [Trait("TestType", "SecurityIntegration")]
    public async Task TC214_LearnerWithoutEnrollment_ShouldNotModifyAnotherLearnersProgress()
    {
        var ownerId = Guid.NewGuid();
        var learnerA = Guid.NewGuid();
        var learnerB = Guid.NewGuid();

        await using var context =
            Tester4TestSupport.CreateContext();

        var skill =
            Tester4TestSupport.CreateSkill();

        var module =
            Tester4TestSupport.CreateModule(
                ownerId,
                skill.SkillId,
                LearningModuleStatusValues.Published);

        var lesson =
            Tester4TestSupport.CreateLesson(
                module.SkillModuleId,
                1);

        var enrollmentB =
            CreateEnrollment(
                learnerB,
                module.SkillModuleId);

        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleLessons.Add(lesson);
        context.SkillModuleEnrollments.Add(enrollmentB);

        await context.SaveChangesAsync();

        var service =
            new LearnerLearningModuleService(
                context,
                new FakeFileStorage());

        var exception =
            await Assert.ThrowsAsync<ConflictException>(() =>
                service.UpdateLessonProgressAsync(
                    learnerA,
                    module.SkillModuleId,
                    lesson.SkillModuleLessonId,
                    new UpdateLessonProgressRequestDto
                    {
                        Status =
                            LearningModuleLessonProgressStatusValues.Completed
                    },
                    CancellationToken.None));

        Assert.Contains(
            "Start the module",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        context.ChangeTracker.Clear();

        var savedForeignEnrollment =
            await context.SkillModuleEnrollments
                .SingleAsync(item =>
                    item.SkillModuleEnrollmentId ==
                    enrollmentB.SkillModuleEnrollmentId);

        Assert.Equal(
            "{}",
            savedForeignEnrollment.LessonProgress);

        Assert.Equal(
            0m,
            savedForeignEnrollment.ProgressPercent);

        Assert.False(
            await context.SkillModuleEnrollments
                .AnyAsync(item =>
                    item.UserId == learnerA));
    }

    private static async Task<ProgressFixture> CreateFixtureAsync()
    {
        var ownerId = Guid.NewGuid();
        var learnerId = Guid.NewGuid();
        var context = Tester4TestSupport.CreateContext();
        var skill = Tester4TestSupport.CreateSkill();
        var module = Tester4TestSupport.CreateModule(ownerId, skill.SkillId, LearningModuleStatusValues.Published);
        var first = Tester4TestSupport.CreateLesson(module.SkillModuleId, 1);
        var second = Tester4TestSupport.CreateLesson(module.SkillModuleId, 2);
        var quiz = Tester4TestSupport.CreateQuiz(module.SkillModuleId, 10);
        var enrollment = CreateEnrollment(learnerId, module.SkillModuleId);

        context.Skills.Add(skill);
        context.SkillModules.Add(module);
        context.SkillModuleLessons.AddRange(first, second);
        context.SkillModuleQuizzes.Add(quiz);
        context.SkillModuleEnrollments.Add(enrollment);
        await context.SaveChangesAsync();

        return new ProgressFixture(context, learnerId, module, [first, second], enrollment);
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

    private sealed record ProgressFixture(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext Context,
        Guid LearnerId,
        SkillModule Module,
        IReadOnlyList<SkillModuleLesson> Lessons,
        SkillModuleEnrollment Enrollment);
}

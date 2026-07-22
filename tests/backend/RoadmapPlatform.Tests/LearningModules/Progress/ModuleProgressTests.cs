using System.Text.Json;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.DTOs.Storage;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Tests.LearningModules.Progress;

public sealed class ModuleProgressTests
{
    [Fact]
    public async Task TC223_GetPublishedModuleBySlugAsync_WhenTwoOfFourUnitsAreCompleted_ShouldReturnFiftyPercentProgress()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new TestApplicationDbContext(options);
        var service = new LearnerLearningModuleService(
            context,
            new StubFileStorage());

        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var moduleSlug = $"module-progress-{Guid.NewGuid():N}";
        var lessonIds = new[]
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        context.AddRange(
            new User
            {
                UserId = userId,
                Username = "progress-learner",
                UsernameNormalized = "PROGRESS-LEARNER",
                Status = "active",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Skill
            {
                SkillId = skillId,
                Name = "Module Progress",
                Slug = $"module-progress-skill-{Guid.NewGuid():N}",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModule
            {
                SkillModuleId = moduleId,
                SkillId = skillId,
                Title = "Module Progress Test",
                Slug = moduleSlug,
                Status = LearningModuleStatusValues.Published,
                PublishedAt = now,
                Metadata = "{}",
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleEnrollment
            {
                SkillModuleEnrollmentId = enrollmentId,
                UserId = userId,
                SkillModuleId = moduleId,
                Status = LearningModuleEnrollmentStatusValues.InProgress,
                StartedAt = now,
                ProgressPercent = 0,
                LessonProgress = JsonSerializer.Serialize(
                    lessonIds.ToDictionary(
                        lessonId => lessonId.ToString(),
                        _ => LearningModuleLessonProgressStatusValues.InProgress)),
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleQuiz
            {
                SkillModuleQuizId = quizId,
                SkillModuleId = moduleId,
                Title = "Module Quiz",
                PassingScorePercent = 60m,
                MaxAttempts = 3,
                Status = LearningModuleStatusValues.Published,
                CreatedAt = now,
                UpdatedAt = now
            },
            new SkillModuleQuizAttempt
            {
                SkillModuleQuizAttemptId = Guid.NewGuid(),
                SkillModuleQuizId = quizId,
                SkillModuleEnrollmentId = enrollmentId,
                UserId = userId,
                AttemptNo = 1,
                Status = LearningModuleQuizAttemptStatusValues.Submitted,
                StartedAt = now.AddMinutes(-10),
                SubmittedAt = now.AddMinutes(-5),
                ScorePercent = 40m,
                EarnedPoints = 2,
                TotalPoints = 5,
                Passed = false
            });

        context.SkillModuleLessons.AddRange(
            lessonIds.Select((lessonId, index) => new SkillModuleLesson
            {
                SkillModuleLessonId = lessonId,
                SkillModuleId = moduleId,
                Title = $"Lesson {index + 1}",
                Slug = $"lesson-{index + 1}",
                OrderIndex = index + 1,
                MarkdownFileKey = $"modules/{moduleId}/lesson-{index + 1}.md",
                ContentVersion = 1,
                IndexingStatus = LearningModuleLessonIndexingStatusValues.Pending,
                CreatedAt = now,
                UpdatedAt = now
            }));

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await service.UpdateLessonProgressAsync(
            userId,
            moduleId,
            lessonIds[0],
            new UpdateLessonProgressRequestDto
            {
                Status = LearningModuleLessonProgressStatusValues.Completed
            },
            CancellationToken.None);

        var secondCompletion = await service.UpdateLessonProgressAsync(
            userId,
            moduleId,
            lessonIds[1],
            new UpdateLessonProgressRequestDto
            {
                Status = LearningModuleLessonProgressStatusValues.Completed
            },
            CancellationToken.None);

        context.ChangeTracker.Clear();

        var detail = await service.GetPublishedModuleBySlugAsync(
            moduleSlug,
            userId,
            CancellationToken.None);

        Assert.Equal(50m, secondCompletion.ProgressPercent);
        Assert.NotNull(detail.Enrollment);
        Assert.Equal(50m, detail.Enrollment.ProgressPercent);
        Assert.Equal(3, detail.Lessons.Count);
        Assert.NotNull(detail.Quiz);
        Assert.Equal(
            LearningModuleEnrollmentStatusValues.InProgress,
            detail.Enrollment.Status);
        Assert.False(await context.SkillModuleQuizAttempts.AnyAsync(attempt =>
            attempt.UserId == userId
            && attempt.SkillModuleQuizId == quizId
            && attempt.Passed == true));
    }

    private sealed class TestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>()
                .Ignore(chunk => chunk.Embedding);
        }
    }

    private sealed class StubFileStorage : IFileStorage
    {
        public string ProviderName => "test";

        public Task<StoredFileDto> SaveAsync(
            string objectPath,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Stream> OpenReadAsync(
            string objectPath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(
            string objectPath,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

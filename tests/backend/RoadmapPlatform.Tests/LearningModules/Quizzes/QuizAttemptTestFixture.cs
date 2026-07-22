using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.DTOs.Storage;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Tests.LearningModules.Quizzes;

internal sealed class QuizAttemptTestFixture : IAsyncDisposable
{
    private QuizAttemptTestFixture(
        ApplicationDbContext context,
        LearnerLearningModuleService service,
        Guid userId,
        Guid moduleId,
        Guid enrollmentId,
        Guid quizId,
        IReadOnlyList<Guid> lessonIds,
        IReadOnlyList<QuizQuestionSeed> questions)
    {
        Context = context;
        Service = service;
        UserId = userId;
        ModuleId = moduleId;
        EnrollmentId = enrollmentId;
        QuizId = quizId;
        LessonIds = lessonIds;
        Questions = questions;
    }

    public ApplicationDbContext Context { get; }

    public LearnerLearningModuleService Service { get; }

    public Guid UserId { get; }

    public Guid ModuleId { get; }

    public Guid EnrollmentId { get; }

    public Guid QuizId { get; }

    public IReadOnlyList<Guid> LessonIds { get; }

    public IReadOnlyList<QuizQuestionSeed> Questions { get; }

    public static async Task<QuizAttemptTestFixture> CreateAsync(
        bool allLessonsCompleted = true,
        int? maxAttempts = 3,
        decimal passingScorePercent = 60m)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new TestApplicationDbContext(options);
        var service = new LearnerLearningModuleService(context, new StubFileStorage());
        var now = DateTime.UtcNow;

        var userId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var moduleId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var lessonIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var user = new User
        {
            UserId = userId,
            Username = "quiz-learner",
            UsernameNormalized = "QUIZ-LEARNER",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        };

        var skill = new Skill
        {
            SkillId = skillId,
            Name = "Unit Testing",
            Slug = $"unit-testing-{Guid.NewGuid():N}",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var module = new SkillModule
        {
            SkillModuleId = moduleId,
            SkillId = skillId,
            Title = "Quiz Attempt Module",
            Slug = $"quiz-attempt-module-{Guid.NewGuid():N}",
            Description = "Module used by quiz attempt unit tests.",
            DifficultyLevel = "beginner",
            Status = LearningModuleStatusValues.Published,
            PublishedAt = now,
            Metadata = "{}",
            CreatedAt = now,
            UpdatedAt = now
        };

        var lessons = lessonIds
            .Select((lessonId, index) => new SkillModuleLesson
            {
                SkillModuleLessonId = lessonId,
                SkillModuleId = moduleId,
                Title = $"Lesson {index + 1}",
                Slug = $"lesson-{index + 1}",
                OrderIndex = index + 1,
                MarkdownFileKey = $"modules/{moduleId}/lesson-{index + 1}.md",
                MarkdownFileName = $"lesson-{index + 1}.md",
                ContentVersion = 1,
                IndexingStatus = LearningModuleLessonIndexingStatusValues.Pending,
                CreatedAt = now,
                UpdatedAt = now
            })
            .ToList();

        var lessonProgress = lessonIds.ToDictionary(
            lessonId => lessonId.ToString(),
            _ => allLessonsCompleted
                ? LearningModuleLessonProgressStatusValues.Completed
                : LearningModuleLessonProgressStatusValues.InProgress);

        if (!allLessonsCompleted)
        {
            lessonProgress[lessonIds[0].ToString()] =
                LearningModuleLessonProgressStatusValues.Completed;
        }

        var enrollment = new SkillModuleEnrollment
        {
            SkillModuleEnrollmentId = enrollmentId,
            UserId = userId,
            SkillModuleId = moduleId,
            Status = LearningModuleEnrollmentStatusValues.InProgress,
            StartedAt = now,
            ProgressPercent = allLessonsCompleted ? 66.67m : 33.33m,
            LessonProgress = JsonSerializer.Serialize(lessonProgress),
            CreatedAt = now,
            UpdatedAt = now
        };

        var quiz = new SkillModuleQuiz
        {
            SkillModuleQuizId = quizId,
            SkillModuleId = moduleId,
            Title = "Module Quiz",
            Description = "Quiz used by unit tests.",
            PassingScorePercent = passingScorePercent,
            MaxAttempts = maxAttempts,
            Status = LearningModuleStatusValues.Published,
            CreatedAt = now,
            UpdatedAt = now
        };

        var questionSeeds = new List<QuizQuestionSeed>();
        var questionEntities = new List<SkillModuleQuizQuestion>();
        var optionEntities = new List<SkillModuleQuizOption>();

        for (var questionIndex = 1; questionIndex <= 2; questionIndex++)
        {
            var questionId = Guid.NewGuid();
            var correctOptionId = Guid.NewGuid();
            var incorrectOptionId = Guid.NewGuid();

            questionSeeds.Add(new QuizQuestionSeed(
                questionId,
                correctOptionId,
                incorrectOptionId));

            questionEntities.Add(new SkillModuleQuizQuestion
            {
                SkillModuleQuizQuestionId = questionId,
                SkillModuleQuizId = quizId,
                QuestionText = $"Question {questionIndex}",
                QuestionType = LearningModuleQuestionTypeValues.SingleChoice,
                OrderIndex = questionIndex,
                Points = 1,
                CreatedAt = now,
                UpdatedAt = now
            });

            optionEntities.AddRange(new[]
            {
                new SkillModuleQuizOption
                {
                    SkillModuleQuizOptionId = correctOptionId,
                    SkillModuleQuizQuestionId = questionId,
                    OptionText = $"Correct option {questionIndex}",
                    IsCorrect = true,
                    OrderIndex = 1,
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new SkillModuleQuizOption
                {
                    SkillModuleQuizOptionId = incorrectOptionId,
                    SkillModuleQuizQuestionId = questionId,
                    OptionText = $"Incorrect option {questionIndex}",
                    IsCorrect = false,
                    OrderIndex = 2,
                    CreatedAt = now,
                    UpdatedAt = now
                }
            });
        }

        context.AddRange(user, skill, module, enrollment, quiz);
        context.SkillModuleLessons.AddRange(lessons);
        context.SkillModuleQuizQuestions.AddRange(questionEntities);
        context.SkillModuleQuizOptions.AddRange(optionEntities);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        return new QuizAttemptTestFixture(
            context,
            service,
            userId,
            moduleId,
            enrollmentId,
            quizId,
            lessonIds,
            questionSeeds);
    }

    public async Task<SkillModuleQuizAttempt> AddInProgressAttemptAsync(int attemptNo = 1)
    {
        var attempt = new SkillModuleQuizAttempt
        {
            SkillModuleQuizAttemptId = Guid.NewGuid(),
            SkillModuleQuizId = QuizId,
            SkillModuleEnrollmentId = EnrollmentId,
            UserId = UserId,
            AttemptNo = attemptNo,
            Status = LearningModuleQuizAttemptStatusValues.InProgress,
            StartedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        Context.SkillModuleQuizAttempts.Add(attempt);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return attempt;
    }

    public async Task<SkillModuleQuizAttempt> AddSubmittedAttemptTodayAsync(
        int attemptNo,
        bool passed = false)
    {
        var submittedAt = DateTime.UtcNow.Date.AddMinutes(attemptNo);
        var attempt = new SkillModuleQuizAttempt
        {
            SkillModuleQuizAttemptId = Guid.NewGuid(),
            SkillModuleQuizId = QuizId,
            SkillModuleEnrollmentId = EnrollmentId,
            UserId = UserId,
            AttemptNo = attemptNo,
            Status = LearningModuleQuizAttemptStatusValues.Submitted,
            StartedAt = submittedAt.AddMinutes(-5),
            SubmittedAt = submittedAt,
            ScorePercent = passed ? 100m : 50m,
            EarnedPoints = passed ? 2 : 1,
            TotalPoints = 2,
            Passed = passed
        };

        Context.SkillModuleQuizAttempts.Add(attempt);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return attempt;
    }

    public ValueTask DisposeAsync() => Context.DisposeAsync();

    internal sealed record QuizQuestionSeed(
        Guid QuestionId,
        Guid CorrectOptionId,
        Guid IncorrectOptionId);

    private sealed class TestApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : ApplicationDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
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

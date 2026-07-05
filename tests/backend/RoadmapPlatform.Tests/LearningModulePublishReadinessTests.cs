using System.Reflection;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Tests;

public sealed class LearningModulePublishReadinessTests
{
    [Fact]
    public void DraftModuleWithoutLessonsCannotPublish()
    {
        var module = CreateModule();

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Errors, error => error.Contains("Add at least 3 lessons", StringComparison.Ordinal));
    }

    [Fact]
    public void ModuleWithIndexedLessonsButNoQuizCannotPublish()
    {
        var module = CreateModule();
        AddIndexedLessons(module, count: 3);

        var readiness = Validate(module);

        Assert.False(readiness.CanPublish);
        Assert.Contains(readiness.Errors, error => error.Contains("Create a quiz", StringComparison.Ordinal));
    }

    [Fact]
    public void CompleteModuleWithIndexedLessonsAndValidQuizCanPublish()
    {
        var module = CreateModule();
        AddIndexedLessons(module, count: 3);
        AddValidQuiz(module, questionCount: 10);

        var readiness = Validate(module);

        Assert.True(readiness.CanPublish);
        Assert.Empty(readiness.Errors);
    }

    private static PublishLearningModuleReadinessDto Validate(SkillModule module)
    {
        var method = typeof(ContentManagerLearningModuleService).GetMethod(
            "ValidatePublishReadiness",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method.Invoke(null, [module]);
        return Assert.IsType<PublishLearningModuleReadinessDto>(result);
    }

    private static SkillModule CreateModule()
    {
        return new SkillModule
        {
            SkillModuleId = Guid.NewGuid(),
            SkillId = Guid.NewGuid(),
            Title = "Testing Seeded Learning Modules",
            Slug = "testing-seeded-learning-modules",
            Status = LearningModuleStatusValues.Draft,
            Metadata = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static void AddIndexedLessons(SkillModule module, int count)
    {
        for (var index = 1; index <= count; index++)
        {
            var lesson = new SkillModuleLesson
            {
                SkillModuleLessonId = Guid.NewGuid(),
                SkillModuleId = module.SkillModuleId,
                Title = $"Lesson {index}",
                Slug = $"lesson-{index}",
                OrderIndex = index,
                MarkdownFileKey = $"learning-modules/{module.SkillModuleId}/lessons/{Guid.NewGuid()}-lesson-{index}.md",
                ContentVersion = 1,
                IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            lesson.SkillModuleChunks.Add(new SkillModuleChunk
            {
                SkillModuleChunkId = Guid.NewGuid(),
                SkillModuleId = module.SkillModuleId,
                SkillModuleLessonId = lesson.SkillModuleLessonId,
                ChunkIndex = 1,
                Content = $"Lesson {index} content",
                TokenCount = 10,
                ContentHash = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            });

            module.SkillModuleLessons.Add(lesson);
        }
    }

    private static void AddValidQuiz(SkillModule module, int questionCount)
    {
        var quiz = new SkillModuleQuiz
        {
            SkillModuleQuizId = Guid.NewGuid(),
            SkillModuleId = module.SkillModuleId,
            Title = "Publish readiness quiz",
            PassingScorePercent = 70,
            Status = LearningModuleStatusValues.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        for (var index = 1; index <= questionCount; index++)
        {
            var question = new SkillModuleQuizQuestion
            {
                SkillModuleQuizQuestionId = Guid.NewGuid(),
                SkillModuleQuizId = quiz.SkillModuleQuizId,
                QuestionText = $"Question {index}",
                QuestionType = LearningModuleQuestionTypeValues.SingleChoice,
                OrderIndex = index,
                Points = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            question.SkillModuleQuizOptions.Add(new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = "Correct answer",
                IsCorrect = true,
                OrderIndex = 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            question.SkillModuleQuizOptions.Add(new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = "Incorrect answer",
                IsCorrect = false,
                OrderIndex = 2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            quiz.SkillModuleQuizQuestions.Add(question);
        }

        module.SkillModuleQuiz = quiz;
    }
}

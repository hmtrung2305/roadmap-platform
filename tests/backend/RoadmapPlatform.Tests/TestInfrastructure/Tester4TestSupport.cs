using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using RoadmapPlatform.Application.DTOs.AiCredits;
using RoadmapPlatform.Application.DTOs.Storage;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Tests.TestInfrastructure;

internal static class Tester4TestSupport
{
    public static ApplicationDbContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new Tester4ApplicationDbContext(options);
    }

    public static T InvokePrivateStatic<T>(Type type, string methodName, params object?[] arguments)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(type.FullName, methodName);

        try
        {
            return (T)(method.Invoke(null, arguments)
                ?? throw new InvalidOperationException($"{methodName} returned null."));
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    public static void InvokePrivateStatic(Type type, string methodName, params object?[] arguments)
    {
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingMethodException(type.FullName, methodName);

        try
        {
            method.Invoke(null, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    public static async Task<T> InvokePrivateAsync<T>(
        object instance,
        string methodName,
        params object?[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new MissingMethodException(instance.GetType().FullName, methodName);

        try
        {
            var result = method.Invoke(instance, arguments);
            if (result is not Task<T> task)
            {
                throw new InvalidOperationException($"{methodName} did not return Task<{typeof(T).Name}>.");
            }

            return await task;
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            throw exception.InnerException;
        }
    }

    public static string ReadRepositoryFile(params string[] relativeParts)
    {
        var root = FindRepositoryRoot();
        var pathParts = new[] { root }.Concat(relativeParts).ToArray();
        return File.ReadAllText(Path.Combine(pathParts));
    }

    public static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src", "backend"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests", "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    public static Skill CreateSkill(Guid? skillId = null, string slug = "csharp")
    {
        var now = DateTime.UtcNow;
        return new Skill
        {
            SkillId = skillId ?? Guid.NewGuid(),
            Name = "C#",
            Slug = slug,
            Category = "Backend",
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SkillModule CreateModule(
        Guid ownerId,
        Guid skillId,
        string status,
        string slug = "csharp-basics")
    {
        var now = DateTime.UtcNow;
        return new SkillModule
        {
            SkillModuleId = Guid.NewGuid(),
            SkillId = skillId,
            Title = "C# Basics",
            Slug = slug,
            Description = "A learning module used by Tester 4.",
            DifficultyLevel = "beginner",
            EstimatedHours = 5,
            Status = status,
            CreatedByUserId = ownerId,
            Metadata = "{}",
            PublishedAt = status == "published" ? now : null,
            ArchivedAt = status == "archived" ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SkillModuleLesson CreateLesson(
        Guid moduleId,
        int orderIndex,
        string? fileKey = null,
        string indexingStatus = "indexed")
    {
        var now = DateTime.UtcNow;
        return new SkillModuleLesson
        {
            SkillModuleLessonId = Guid.NewGuid(),
            SkillModuleId = moduleId,
            Title = $"Lesson {orderIndex}",
            Slug = $"lesson-{orderIndex}",
            Summary = $"Lesson {orderIndex} summary",
            OrderIndex = orderIndex,
            EstimatedHours = 1,
            MarkdownFileKey = fileKey ?? $"learning-modules/{moduleId}/lesson-{orderIndex}.md",
            MarkdownFileName = $"lesson-{orderIndex}.md",
            ContentHash = Guid.NewGuid().ToString("N"),
            ContentSizeBytes = 100,
            ContentVersion = 1,
            IndexingStatus = indexingStatus,
            IndexedAt = indexingStatus == "indexed" ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public static SkillModuleQuiz CreateQuiz(Guid moduleId, int questionCount)
    {
        var now = DateTime.UtcNow;
        var quiz = new SkillModuleQuiz
        {
            SkillModuleQuizId = Guid.NewGuid(),
            SkillModuleId = moduleId,
            Title = "Module quiz",
            PassingScorePercent = 70,
            Status = "draft",
            CreatedAt = now,
            UpdatedAt = now
        };

        for (var index = 1; index <= questionCount; index++)
        {
            var question = new SkillModuleQuizQuestion
            {
                SkillModuleQuizQuestionId = Guid.NewGuid(),
                SkillModuleQuizId = quiz.SkillModuleQuizId,
                QuestionText = $"Question {index}",
                QuestionType = "single_choice",
                OrderIndex = index,
                Points = 1,
                CreatedAt = now,
                UpdatedAt = now
            };

            question.SkillModuleQuizOptions.Add(new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = "Correct",
                IsCorrect = true,
                OrderIndex = 1,
                CreatedAt = now,
                UpdatedAt = now
            });

            question.SkillModuleQuizOptions.Add(new SkillModuleQuizOption
            {
                SkillModuleQuizOptionId = Guid.NewGuid(),
                SkillModuleQuizQuestionId = question.SkillModuleQuizQuestionId,
                OptionText = "Incorrect",
                IsCorrect = false,
                OrderIndex = 2,
                CreatedAt = now,
                UpdatedAt = now
            });

            quiz.SkillModuleQuizQuestions.Add(question);
        }

        return quiz;
    }

    public static void AddIndexedChunk(SkillModuleLesson lesson)
    {
        lesson.SkillModuleChunks.Add(new SkillModuleChunk
        {
            SkillModuleChunkId = Guid.NewGuid(),
            SkillModuleId = lesson.SkillModuleId,
            SkillModuleLessonId = lesson.SkillModuleLessonId,
            ChunkIndex = 1,
            Content = $"Indexed content for {lesson.Title}",
            TokenCount = 10,
            ContentHash = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        });
    }
}

internal sealed class Tester4ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<SkillModuleChunk>().Ignore(chunk => chunk.Embedding);
    }
}

internal sealed class FakeFileStorage : IFileStorage
{
    private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

    public string ProviderName => "tester4-memory";

    public IReadOnlyCollection<string> DeletedObjectPaths => _deletedObjectPaths;
    private readonly List<string> _deletedObjectPaths = [];

    public void Seed(string objectPath, string content)
    {
        _files[objectPath] = Encoding.UTF8.GetBytes(content);
    }

    public async Task<StoredFileDto> SaveAsync(
        string objectPath,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();
        _files[objectPath] = bytes;
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        return new StoredFileDto(objectPath, null, bytes.LongLength, hash);
    }

    public Task<Stream> OpenReadAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        if (!_files.TryGetValue(objectPath, out var bytes))
        {
            throw new FileNotFoundException("Test file was not found.", objectPath);
        }

        return Task.FromResult<Stream>(new MemoryStream(bytes, writable: false));
    }

    public Task DeleteAsync(
        string objectPath,
        CancellationToken cancellationToken = default)
    {
        _files.Remove(objectPath);
        _deletedObjectPaths.Add(objectPath);
        return Task.CompletedTask;
    }
}

internal sealed class RecordingAiCreditService : IAiCreditService
{
    public int RemainingCredits { get; set; } = 10;
    public bool ThrowLimitExceeded { get; set; }
    public int SpendCalls { get; private set; }
    public string? LastFeatureName { get; private set; }
    public int LastCreditCost { get; private set; }
    public Guid? LastRequestRefId { get; private set; }

    public Task<AiCreditStatusDto> GetStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateStatus(RemainingCredits));
    }

    public Task<AiCreditStatusDto> SpendAsync(
        Guid userId,
        string featureName,
        int creditCost,
        Guid? requestRefId = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        SpendCalls++;
        LastFeatureName = featureName;
        LastCreditCost = creditCost;
        LastRequestRefId = requestRefId;

        if (ThrowLimitExceeded || RemainingCredits < creditCost)
        {
            throw new AiCreditLimitExceededException(CreateStatus(RemainingCredits));
        }

        RemainingCredits -= creditCost;
        return Task.FromResult(CreateStatus(RemainingCredits));
    }

    private static AiCreditStatusDto CreateStatus(int remainingCredits)
    {
        return new AiCreditStatusDto
        {
            PlanCode = "free",
            DailyCreditLimit = 10,
            UsedCreditsToday = 10 - remainingCredits,
            RemainingCreditsToday = remainingCredits,
            ResetAt = DateTimeOffset.UtcNow.AddDays(1)
        };
    }
}

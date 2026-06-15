using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleIndexingWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan StaleIndexingTimeout = TimeSpan.FromMinutes(15);
    private const int BatchSize = 5;
    private const int MaxErrorLength = 1000;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LearningModuleIndexingWorker> _logger;

    public LearningModuleIndexingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<LearningModuleIndexingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingLessonsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Learning module indexing worker failed while polling.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingLessonsAsync(CancellationToken cancellationToken)
    {
        List<Guid> lessonIds;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var staleBefore = DateTime.UtcNow.Subtract(StaleIndexingTimeout);

            lessonIds = await context.SkillModuleLessons
                .AsNoTracking()
                .Where(lesson =>
                    lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.Pending
                    || lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.NeedsReindex
                    || (
                        lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.Indexing
                        && lesson.UpdatedAt < staleBefore))
                .OrderBy(lesson => lesson.UpdatedAt)
                .Select(lesson => lesson.SkillModuleLessonId)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);
        }

        foreach (var lessonId in lessonIds)
        {
            await ProcessLessonAsync(lessonId, cancellationToken);
        }
    }

    private async Task ProcessLessonAsync(
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var lesson = await context.SkillModuleLessons
            .FirstOrDefaultAsync(item => item.SkillModuleLessonId == lessonId, cancellationToken);

        if (lesson == null)
        {
            return;
        }

        ILearningModuleRagIndexingService indexingService;

        try
        {
            indexingService = scope.ServiceProvider.GetRequiredService<ILearningModuleRagIndexingService>();
        }
        catch (Exception ex)
        {
            await MarkLessonFailedAsync(
                context,
                lesson,
                ex,
                cancellationToken);

            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            var expectedContentVersion = lesson.ContentVersion;
            var expectedContentHash = lesson.ContentHash;
            var expectedMarkdownFileKey = lesson.MarkdownFileKey;

            lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexing;
            lesson.IndexingError = null;
            lesson.IndexedAt = null;
            lesson.UpdatedAt = now;

            await context.SaveChangesAsync(cancellationToken);

            await using var stream = await fileStorage.OpenReadAsync(
                expectedMarkdownFileKey,
                cancellationToken);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var markdown = await reader.ReadToEndAsync(cancellationToken);

            await indexingService.IndexLessonAsync(
                lesson.SkillModuleId,
                lesson.SkillModuleLessonId,
                markdown,
                expectedContentVersion,
                expectedContentHash,
                cancellationToken);

            await context.Entry(lesson).ReloadAsync(cancellationToken);

            if (!IsSameLessonContent(
                lesson,
                expectedContentVersion,
                expectedContentHash,
                expectedMarkdownFileKey))
            {
                _logger.LogInformation(
                    "Skipped marking lesson {LessonId} as indexed because the lesson file changed while indexing.",
                    lesson.SkillModuleLessonId);

                return;
            }

            lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed;
            lesson.IndexingError = null;
            lesson.IndexedAt = DateTime.UtcNow;
            lesson.UpdatedAt = lesson.IndexedAt.Value;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (LearningModuleIndexingSkippedException ex)
        {
            await HandleSkippedIndexingAsync(
                context,
                lesson,
                ex,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await MarkLessonFailedAsync(
                context,
                lesson,
                ex,
                cancellationToken);

            _logger.LogError(
                ex,
                "Failed to index learning module lesson {LessonId}.",
                lesson.SkillModuleLessonId);
        }
    }

    private static async Task HandleSkippedIndexingAsync(
        ApplicationDbContext context,
        SkillModuleLesson lesson,
        LearningModuleIndexingSkippedException exception,
        CancellationToken cancellationToken)
    {
        await context.Entry(lesson).ReloadAsync(cancellationToken);

        if (lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.Indexing)
        {
            lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Pending;
            lesson.IndexingError = null;
            lesson.IndexedAt = null;
            lesson.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsSameLessonContent(
        SkillModuleLesson lesson,
        int expectedContentVersion,
        string? expectedContentHash,
        string expectedMarkdownFileKey)
    {
        return lesson.ContentVersion == expectedContentVersion
            && string.Equals(
                lesson.ContentHash,
                expectedContentHash,
                StringComparison.Ordinal)
            && string.Equals(
                lesson.MarkdownFileKey,
                expectedMarkdownFileKey,
                StringComparison.Ordinal);
    }

    private static async Task MarkLessonFailedAsync(
        ApplicationDbContext context,
        SkillModuleLesson lesson,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Failed;
        lesson.IndexingError = CreateErrorMessage(exception);
        lesson.IndexedAt = null;
        lesson.UpdatedAt = now;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string CreateErrorMessage(Exception exception)
    {
        var message = exception.Message.Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            message = "Lesson indexing failed.";
        }

        return message.Length <= MaxErrorLength
            ? message
            : message[..MaxErrorLength];
    }
}

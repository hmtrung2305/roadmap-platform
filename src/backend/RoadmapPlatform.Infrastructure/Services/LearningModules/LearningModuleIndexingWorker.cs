using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Application.Interfaces.Storage;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Data;
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
        var lessonIds = await ClaimPendingLessonsAsync(cancellationToken);

        foreach (var lessonId in lessonIds)
        {
            await ProcessLessonAsync(lessonId, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<Guid>> ClaimPendingLessonsAsync(
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = context.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
            await using var command = connection.CreateCommand();

            command.Transaction = transaction;
            command.CommandText = """
                WITH candidates AS (
                    SELECT lesson.skill_module_lesson_id
                    FROM public.skill_module_lesson lesson
                    WHERE lesson.indexing_status = @pending_status
                       OR lesson.indexing_status = @needs_reindex_status
                       OR (
                            lesson.indexing_status = @indexing_status
                            AND lesson.updated_at < @stale_before
                       )
                    ORDER BY lesson.updated_at, lesson.skill_module_lesson_id
                    LIMIT @batch_size
                    FOR UPDATE SKIP LOCKED
                )
                UPDATE public.skill_module_lesson lesson
                SET indexing_status = @indexing_status,
                    indexing_error = NULL,
                    indexed_at = NULL,
                    updated_at = @claimed_at
                FROM candidates
                WHERE lesson.skill_module_lesson_id = candidates.skill_module_lesson_id
                RETURNING lesson.skill_module_lesson_id;
                """;

            AddParameter(command, "pending_status", LearningModuleLessonIndexingStatusValues.Pending);
            AddParameter(command, "needs_reindex_status", LearningModuleLessonIndexingStatusValues.NeedsReindex);
            AddParameter(command, "indexing_status", LearningModuleLessonIndexingStatusValues.Indexing);
            AddParameter(command, "stale_before", DateTime.UtcNow.Subtract(StaleIndexingTimeout));
            AddParameter(command, "claimed_at", DateTime.UtcNow);
            AddParameter(command, "batch_size", BatchSize);

            var lessonIds = new List<Guid>(BatchSize);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                lessonIds.Add(reader.GetGuid(0));
            }

            await reader.DisposeAsync();
            await transaction.CommitAsync(cancellationToken);

            return lessonIds;
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
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

        if (lesson == null
            || lesson.IndexingStatus != LearningModuleLessonIndexingStatusValues.Indexing)
        {
            return;
        }

        var expectedContentVersion = lesson.ContentVersion;
        var expectedContentHash = lesson.ContentHash;
        var expectedMarkdownFileKey = lesson.MarkdownFileKey;

        ILearningModuleRagIndexingService indexingService;

        try
        {
            indexingService = scope.ServiceProvider.GetRequiredService<ILearningModuleRagIndexingService>();
        }
        catch (Exception ex)
        {
            await MarkLessonFailedIfCurrentAsync(
                context,
                lesson,
                expectedContentVersion,
                expectedContentHash,
                expectedMarkdownFileKey,
                ex,
                cancellationToken);

            return;
        }

        try
        {
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

            if (!IsCurrentClaim(
                lesson,
                expectedContentVersion,
                expectedContentHash,
                expectedMarkdownFileKey))
            {
                _logger.LogInformation(
                    "Skipped marking lesson {LessonId} as indexed because its content or indexing state changed.",
                    lesson.SkillModuleLessonId);

                return;
            }

            lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Indexed;
            lesson.IndexingError = null;
            lesson.IndexedAt = DateTime.UtcNow;
            lesson.UpdatedAt = lesson.IndexedAt.Value;

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (LearningModuleIndexingSkippedException)
        {
            await RequeueLessonIfCurrentAsync(
                context,
                lesson,
                expectedContentVersion,
                expectedContentHash,
                expectedMarkdownFileKey,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await MarkLessonFailedIfCurrentAsync(
                context,
                lesson,
                expectedContentVersion,
                expectedContentHash,
                expectedMarkdownFileKey,
                ex,
                cancellationToken);

            _logger.LogError(
                ex,
                "Failed to index learning module lesson {LessonId}.",
                lesson.SkillModuleLessonId);
        }
    }

    private static async Task RequeueLessonIfCurrentAsync(
        ApplicationDbContext context,
        SkillModuleLesson lesson,
        int expectedContentVersion,
        string? expectedContentHash,
        string expectedMarkdownFileKey,
        CancellationToken cancellationToken)
    {
        await context.Entry(lesson).ReloadAsync(cancellationToken);

        if (!IsCurrentClaim(
            lesson,
            expectedContentVersion,
            expectedContentHash,
            expectedMarkdownFileKey))
        {
            return;
        }

        lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Pending;
        lesson.IndexingError = null;
        lesson.IndexedAt = null;
        lesson.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task MarkLessonFailedIfCurrentAsync(
        ApplicationDbContext context,
        SkillModuleLesson lesson,
        int expectedContentVersion,
        string? expectedContentHash,
        string expectedMarkdownFileKey,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await context.Entry(lesson).ReloadAsync(cancellationToken);

        if (!IsCurrentClaim(
            lesson,
            expectedContentVersion,
            expectedContentHash,
            expectedMarkdownFileKey))
        {
            return;
        }

        lesson.IndexingStatus = LearningModuleLessonIndexingStatusValues.Failed;
        lesson.IndexingError = CreateErrorMessage(exception);
        lesson.IndexedAt = null;
        lesson.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsCurrentClaim(
        SkillModuleLesson lesson,
        int expectedContentVersion,
        string? expectedContentHash,
        string expectedMarkdownFileKey)
    {
        return lesson.IndexingStatus == LearningModuleLessonIndexingStatusValues.Indexing
            && lesson.ContentVersion == expectedContentVersion
            && string.Equals(
                lesson.ContentHash,
                expectedContentHash,
                StringComparison.Ordinal)
            && string.Equals(
                lesson.MarkdownFileKey,
                expectedMarkdownFileKey,
                StringComparison.Ordinal);
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
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

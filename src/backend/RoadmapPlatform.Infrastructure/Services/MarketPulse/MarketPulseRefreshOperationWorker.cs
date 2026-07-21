using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

/// <summary>
/// Durable coordinator for TopCV crawler -> .NET import -> analytics ready.
/// State is persisted before every external boundary so API reloads do not lose progress.
/// </summary>
public sealed class MarketPulseRefreshOperationWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MarketPulseSettings> settings,
    ILogger<MarketPulseRefreshOperationWorker> logger) : BackgroundService
{
    private const long WorkerAdvisoryLockKey = 6_418_325_908_117_204_773L;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessNextAsync(stoppingToken);
                await Task.Delay(processed ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(3), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "TopCV refresh-operation worker failed.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var usesPostgresLock = string.Equals(
            db.Database.ProviderName,
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            StringComparison.Ordinal);
        if (usesPostgresLock && !await TryAcquireWorkerLockAsync(db, cancellationToken))
        {
            return false;
        }

        MarketPulsePipelineRun? operation = null;
        try
        {
            operation = await db.Set<MarketPulsePipelineRun>()
                .OrderBy(item => item.RequestedAt)
                .FirstOrDefaultAsync(item =>
                    item.OperationType == "refresh" &&
                    (item.Status == "queued" || item.Status == "crawling" || item.Status == "importing"),
                    cancellationToken);
            if (operation is null)
            {
                return false;
            }

            if (operation.Status == "queued")
            {
                operation.Status = "crawling";
                operation.CurrentStep = "crawler";
                operation.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
                var client = scope.ServiceProvider.GetRequiredService<TopCvJobsApiClient>();
                var trigger = await client.TriggerListingCrawlAsync(cancellationToken);
                if (!trigger.Accepted)
                {
                    await FailAsync(db, operation, "CRAWLER_TRIGGER_FAILED", trigger.Error, cancellationToken);
                    return true;
                }
                // Poll on the next worker tick. If the process stops after persisting
                // `crawling` but before the HTTP call, the crawling branch retries safely.
                return true;
            }

            if (operation.Status == "crawling")
            {
                var healthService = scope.ServiceProvider.GetRequiredService<IJobsApiHealthService>();
                var health = await healthService.GetHealthAsync(cancellationToken);
                var operationStartedAt = NormalizeUtc(operation.StartedAt);
                var hasOperationCrawlerRun = health.LatestListingStartedAt.HasValue &&
                    NormalizeUtc(health.LatestListingStartedAt.Value) >= operationStartedAt;
                if (hasOperationCrawlerRun && IsUsablePartialCrawlerResult(health))
                {
                    operation.CrawlerSuccessAt = NormalizeUtc(
                        health.LatestListingFinishedAt ?? health.LatestListingStartedAt!.Value);
                    operation.Status = "importing";
                    operation.CurrentStep = "import";
                    operation.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return true;
                }
                else if (health.IsBlocked || health.PagesBlocked > 0)
                {
                    await FailAsync(
                        db,
                        operation,
                        "CRAWLER_BLOCKED",
                        "TopCV blocked the crawl before any usable listing data was collected.",
                        cancellationToken);
                    return true;
                }
                else if (hasOperationCrawlerRun && IsTerminalCrawlerFailure(health.LatestListingStatus))
                {
                    await FailAsync(
                        db,
                        operation,
                        "CRAWLER_INCOMPLETE",
                        $"TopCV listing crawl ended with status '{health.LatestListingStatus}'; import was not started.",
                        cancellationToken);
                    return true;
                }
                if (health.LatestSuccessfulCrawlAt.HasValue &&
                    NormalizeUtc(health.LatestSuccessfulCrawlAt.Value) > operation.BaselineCrawlerSuccessAt &&
                    string.Equals(health.LatestListingStatus, "success", StringComparison.OrdinalIgnoreCase))
                {
                    operation.CrawlerSuccessAt = NormalizeUtc(health.LatestSuccessfulCrawlAt.Value);
                    operation.Status = "importing";
                    operation.CurrentStep = "import";
                    operation.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }
                else if (DateTime.UtcNow - operation.StartedAt >
                         TimeSpan.FromMinutes(Math.Clamp(settings.Value.RefreshOperationTimeoutMinutes, 1, 120)))
                {
                    await FailAsync(db, operation, "CRAWLER_TIMEOUT", "No newer successful TopCV crawl arrived before timeout.", cancellationToken);
                    return true;
                }
                else if (!hasOperationCrawlerRun)
                {
                    // This also closes the crash window between persisting `crawling` and
                    // sending the trigger. Python returns 409 while a run is already active;
                    // the client treats that as an idempotent attachment to the active run.
                    var client = scope.ServiceProvider.GetRequiredService<TopCvJobsApiClient>();
                    var trigger = await client.TriggerListingCrawlAsync(cancellationToken);
                    if (!trigger.Accepted)
                    {
                        await FailAsync(db, operation, "CRAWLER_TRIGGER_FAILED", trigger.Error, cancellationToken);
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }

            if (operation.Status == "importing")
            {
                var service = scope.ServiceProvider.GetRequiredService<IMarketPulseService>();
                MarketPulseRefreshResultDto result;
                try
                {
                    result = await service.RefreshAsync(cancellationToken);
                }
                catch (InvalidOperationException exception) when (
                    exception.Message.Contains(
                        "refresh is already running",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // A scheduled/manual import in this process owns the in-memory gate.
                    // Keep the durable operation importing and retry on the next tick.
                    operation.UpdatedAt = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                    return true;
                }
                var replayedSuccessfulObservation = result.Status == "skipped" &&
                    result.LifecycleSkippedReason == "source_observation_not_newer";
                var usablePartialImport = result.Status == "partial" &&
                    result.IsSourceFresh &&
                    result.PostingsScraped > 0;
                if ((!replayedSuccessfulObservation &&
                     result.Status is not "success" and not "empty" &&
                     !usablePartialImport) ||
                    !result.IsSourceFresh)
                {
                    await FailAsync(
                        db,
                        operation,
                        "IMPORT_INCOMPLETE",
                        "TopCV import did not produce a fresh usable batch.",
                        cancellationToken);
                    return true;
                }
                operation.ImportRunId = result.RunId;
                operation.Status = "success";
                operation.CurrentStep = "analytics";
                operation.FinishedAt = DateTime.UtcNow;
                operation.UpdatedAt = operation.FinishedAt.Value;
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception exception) when (
            exception is not OperationCanceledException && operation is not null)
        {
            logger.LogWarning(
                exception,
                "TopCV refresh operation {OperationId} failed.",
                operation.MarketPulsePipelineRunId);
            await FailAsync(
                db,
                operation,
                "PIPELINE_FAILED",
                exception.Message,
                cancellationToken);
            return true;
        }
        finally
        {
            if (usesPostgresLock)
            {
                try
                {
                    await ReleaseWorkerLockAsync(db, CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Could not explicitly release the Market Pulse worker lock.");
                }
                finally
                {
                    await db.Database.CloseConnectionAsync();
                }
            }
        }
    }

    private static async Task<bool> TryAcquireWorkerLockAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Database.OpenConnectionAsync(cancellationToken);
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT pg_try_advisory_lock(@lock_key)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.Value = WorkerAdvisoryLockKey;
        command.Parameters.Add(parameter);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        var acquired = result is true;
        if (!acquired)
        {
            await db.Database.CloseConnectionAsync();
        }
        return acquired;
    }

    private static async Task ReleaseWorkerLockAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@lock_key)";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "lock_key";
        parameter.Value = WorkerAdvisoryLockKey;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task FailAsync(
        ApplicationDbContext db,
        MarketPulsePipelineRun operation,
        string code,
        string? message,
        CancellationToken cancellationToken)
    {
        // RefreshAsync clears its shared scoped change tracker while recording an
        // import failure. Re-query by the durable ID so a detached worker entity
        // cannot leave the operation permanently stuck in `importing`.
        var operationId = operation.MarketPulsePipelineRunId;
        var persisted = await db.Set<MarketPulsePipelineRun>()
            .SingleOrDefaultAsync(
                item => item.OperationType == "refresh" && item.MarketPulsePipelineRunId == operationId,
                cancellationToken);
        if (persisted is null)
        {
            return;
        }

        persisted.Status = "failed";
        persisted.ErrorCode = code;
        persisted.ErrorMessage = message;
        persisted.FinishedAt = DateTime.UtcNow;
        persisted.UpdatedAt = persisted.FinishedAt.Value;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static bool IsTerminalCrawlerFailure(string? value) =>
        value?.Trim().ToLowerInvariant() is
            "failed" or
            "partial" or
            "blocked" or
            "layout_changed" or
            "empty_protected";

    private static bool IsUsablePartialCrawlerResult(MarketPulseExternalSourceHealthDto health) =>
        string.Equals(
            health.LatestListingStatus,
            "partial_success",
            StringComparison.OrdinalIgnoreCase) &&
        health.LatestListingFinishedAt.HasValue &&
        health.LatestListingJobsSeen > 0;
}

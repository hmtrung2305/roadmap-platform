using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Extensions;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services
    .AddApplicationServices()
    .AddInfrastructureServices(builder.Configuration);

using var host = builder.Build();

var logger = host.Services
    .GetRequiredService<ILoggerFactory>()
    .CreateLogger("MarketPulseCronJob");

try
{
    logger.LogInformation("Starting Market Pulse cron refresh at {StartedAtUtc} UTC.", DateTimeOffset.UtcNow);

    using var scope = host.Services.CreateScope();
    var marketPulseService = scope.ServiceProvider.GetRequiredService<IMarketPulseService>();
    var mode = ReadArgument(args, "--mode") ?? "import";
    var historySyncRequest = BuildHistorySyncRequest(args);
    var result = string.Equals(mode, "history-sync", StringComparison.OrdinalIgnoreCase)
        ? await marketPulseService.SyncPublicationHistoryAsync(
            historySyncRequest,
            CancellationToken.None)
        : await marketPulseService.RefreshAsync(CancellationToken.None);

    LogResult(logger, result);

    if (!IsSuccessfulRefresh(result, mode))
    {
        logger.LogError(
            "Market Pulse refresh produced no acceptable data: status={Status}, fetchStatus={FetchStatus}, sourceFresh={IsSourceFresh}, fetched={PostingsScraped}. Returning a non-zero exit code.",
            result.Status,
            result.FetchStatus,
            result.IsSourceFresh,
            result.PostingsScraped);
        return 2;
    }

    logger.LogInformation("Market Pulse cron refresh finished successfully at {FinishedAtUtc} UTC.", DateTimeOffset.UtcNow);
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Market Pulse cron refresh failed.");
    return 1;
}

static MarketPulseHistorySyncRequestDto? BuildHistorySyncRequest(string[] arguments)
{
    var hasLookback = int.TryParse(ReadArgument(arguments, "--lookback-days"), out var lookbackDays);
    var hasPageSize = int.TryParse(ReadArgument(arguments, "--page-size"), out var pageSize);
    var hasMaxItems = int.TryParse(ReadArgument(arguments, "--max-items"), out var maxItems);
    return hasLookback || hasPageSize || hasMaxItems
        ? new MarketPulseHistorySyncRequestDto
        {
            LookbackDays = hasLookback ? lookbackDays : null,
            JobsApiPageSize = hasPageSize ? pageSize : null,
            JobsApiMaxItems = hasMaxItems ? maxItems : null
        }
        : null;
}

static string? ReadArgument(string[] arguments, string name)
{
    for (var index = 0; index < arguments.Length; index++)
    {
        if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase) &&
            index + 1 < arguments.Length)
        {
            return arguments[index + 1];
        }
        var prefix = name + "=";
        if (arguments[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[index][prefix.Length..];
        }
    }
    return null;
}

static bool IsSuccessfulRefresh(MarketPulseRefreshResultDto result, string mode)
{
    if (string.Equals(mode, "history-sync", StringComparison.OrdinalIgnoreCase))
    {
        return result.IsSourceFresh && result.IsCompleteSync &&
            result.Status is "success" or "empty";
    }
    if (string.Equals(result.Status, "skipped", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(
            result.LifecycleSkippedReason,
            "source_observation_not_newer",
            StringComparison.OrdinalIgnoreCase))
    {
        return result.IsSourceFresh && result.IsCompleteSync;
    }

    if (!result.IsSourceFresh || !result.IsCompleteSync || !result.MissingLifecycleApplied)
    {
        return false;
    }

    return string.Equals(result.Status, "empty", StringComparison.OrdinalIgnoreCase) ||
        (string.Equals(result.Status, "success", StringComparison.OrdinalIgnoreCase) &&
         result.PostingsScraped > 0);
}

static void LogResult(ILogger logger, MarketPulseRefreshResultDto result)
{
    var syncType = result.IsCompleteSync ? "full" : "partial";
    var lifecycleOutcome = result.MissingLifecycleApplied ? "executed" : "skipped";
    logger.LogInformation(
        "Market Pulse result: status={Status}, fetchStatus={FetchStatus}, sourceFresh={IsSourceFresh}, sourceGeneratedAt={SourceGeneratedAt}, latestSuccessfulCrawlAt={LatestSuccessfulCrawlAt}, fetched={PostingsScraped}, total={SourceTotal}, syncType={SyncType}, lifecycle={LifecycleOutcome}, lifecycleSkippedReason={LifecycleSkippedReason}, inserted={PostingsInserted}, updated={PostingsUpdated}, seen={PostingsSeen}, expiredInRun={PostingsExpired}, active={ActivePostings}, stale={StalePostings}, expired={ExpiredPostings}.",
        result.Status,
        result.FetchStatus,
        result.IsSourceFresh,
        result.SourceGeneratedAt,
        result.LatestSuccessfulCrawlAt,
        result.PostingsScraped,
        result.SourceTotal,
        syncType,
        lifecycleOutcome,
        result.LifecycleSkippedReason,
        result.PostingsInserted,
        result.PostingsUpdated,
        result.PostingsSeen,
        result.PostingsExpired,
        result.ActivePostings,
        result.StalePostings,
        result.ExpiredPostings);
}

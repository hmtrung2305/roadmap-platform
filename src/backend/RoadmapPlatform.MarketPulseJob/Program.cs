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
    var result = await marketPulseService.RefreshAsync(CancellationToken.None);

    LogResult(logger, result);

    logger.LogInformation("Market Pulse cron refresh finished successfully at {FinishedAtUtc} UTC.", DateTimeOffset.UtcNow);
    return 0;
}
catch (Exception ex)
{
    logger.LogError(ex, "Market Pulse cron refresh failed.");
    return 1;
}

static void LogResult(ILogger logger, MarketPulseRefreshResultDto result)
{
    logger.LogInformation(
        "Market Pulse result: status={Status}, fetchStatus={FetchStatus}, sourceGeneratedAt={SourceGeneratedAt}, latestSuccessfulCrawlAt={LatestSuccessfulCrawlAt}, fetched={PostingsScraped}, total={SourceTotal}, completeSync={IsCompleteSync}, lifecycleApplied={MissingLifecycleApplied}, lifecycleSkippedReason={LifecycleSkippedReason}, inserted={PostingsInserted}, updated={PostingsUpdated}, seen={PostingsSeen}, expiredInRun={PostingsExpired}, active={ActivePostings}, stale={StalePostings}, expired={ExpiredPostings}.",
        result.Status,
        result.FetchStatus,
        result.SourceGeneratedAt,
        result.LatestSuccessfulCrawlAt,
        result.PostingsScraped,
        result.SourceTotal,
        result.IsCompleteSync,
        result.MissingLifecycleApplied,
        result.LifecycleSkippedReason,
        result.PostingsInserted,
        result.PostingsUpdated,
        result.PostingsSeen,
        result.PostingsExpired,
        result.ActivePostings,
        result.StalePostings,
        result.ExpiredPostings);
}

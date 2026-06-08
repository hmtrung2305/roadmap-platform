using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

builder.Services.AddInfrastructureServices(builder.Configuration);

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
        "Market Pulse result: snapshotDate={SnapshotDate}, sources={SourcesScraped}, scraped={PostingsScraped}, saved={PostingsSaved}, new={NewPostings}, updated={UpdatedPostings}, active={ActivePostings}, stale={StalePostings}, expired={ExpiredPostings}, skillSnapshots={SkillSnapshotsSaved}.",
        result.SnapshotDate,
        result.SourcesScraped,
        result.PostingsScraped,
        result.PostingsSaved,
        result.NewPostings,
        result.UpdatedPostings,
        result.ActivePostings,
        result.StalePostings,
        result.ExpiredPostings,
        result.SkillSnapshotsSaved);
}
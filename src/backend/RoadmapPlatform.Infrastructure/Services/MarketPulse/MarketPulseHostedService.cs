using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class MarketPulseHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<MarketPulseSettings> options,
    ILogger<MarketPulseHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogInformation("Market Pulse scheduler is disabled.");
            return;
        }

        if (settings.RunOnStartup)
        {
            await RefreshAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = CalculateDelay(settings.DailyRunTime);
            logger.LogInformation("Next Market Pulse refresh in {Delay}.", delay);

            await Task.Delay(delay, stoppingToken);
            await RefreshAsync(stoppingToken);
        }
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IMarketPulseService>();
            var result = await service.RefreshAsync(cancellationToken);

            logger.LogInformation(
                "Market Pulse refreshed {PostingsScraped} postings from {SourcesScraped} sources; new={NewPostings}, updated={UpdatedPostings}, active={ActivePostings}, stale={StalePostings}, expired={ExpiredPostings}, skillSnapshots={SkillSnapshotsSaved}.",
                result.PostingsScraped,
                result.SourcesScraped,
                result.NewPostings,
                result.UpdatedPostings,
                result.ActivePostings,
                result.StalePostings,
                result.ExpiredPostings,
                result.SkillSnapshotsSaved);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Market Pulse refresh failed.");
        }
    }

    private static TimeSpan CalculateDelay(string dailyRunTime)
    {
        if (!TimeSpan.TryParse(dailyRunTime, out var runAt))
        {
            runAt = new TimeSpan(2, 30, 0);
        }

        var now = DateTimeOffset.Now;
        var nextRun = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset).Add(runAt);

        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        return nextRun - now;
    }
}

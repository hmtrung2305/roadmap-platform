using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Application.Models.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public sealed class JobsApiJobMarketSnapshotProvider(
    JobsApiClient jobsApiClient,
    IOptions<MarketPulseSettings> options) : IJobMarketSnapshotProvider
{
    public async Task<JobMarketSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var activeTask = jobsApiClient.FetchAsync(settings.ActiveJobsApiUrl, cancellationToken);
        var todayTask = jobsApiClient.FetchAsync(settings.TodayJobsApiUrl, cancellationToken);

        await Task.WhenAll(activeTask, todayTask);

        var active = await activeTask;
        var today = await todayTask;

        return new JobMarketSnapshot
        {
            ActiveTotal = active.Total,
            TodayTotal = today.Total,
            ActiveJobs = active.Jobs,
            TodayJobs = today.Jobs
        };
    }
}
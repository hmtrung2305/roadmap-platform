using RoadmapPlatform.Application.Models.MarketPulse;

namespace RoadmapPlatform.Application.Interfaces.MarketPulse;

public interface IJobMarketSnapshotProvider
{
    Task<JobMarketSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken);
}
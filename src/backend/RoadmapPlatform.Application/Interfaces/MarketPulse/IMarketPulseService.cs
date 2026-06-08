using RoadmapPlatform.Application.DTOs.MarketPulse;

namespace RoadmapPlatform.Application.Interfaces.MarketPulse;

public interface IMarketPulseService
{
    Task<MarketPulseOverviewDto> GetOverviewAsync(
        int days,
        IReadOnlyCollection<string> skillSlugs,
        CancellationToken cancellationToken);

    Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken);
}

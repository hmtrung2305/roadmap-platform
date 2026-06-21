using RoadmapPlatform.Application.DTOs.MarketPulse;

namespace RoadmapPlatform.Application.Interfaces.MarketPulse;

public interface IMarketPulseService
{
    Task<MarketPulseOverviewDto> GetOverviewAsync(
        MarketPulseOverviewQueryDto query,
        CancellationToken cancellationToken);

    Task<MarketPulseRefreshResultDto> RefreshAsync(CancellationToken cancellationToken);

    Task<MarketPulseRefreshResultDto> IngestAsync(
        MarketPulseIngestRequestDto request,
        CancellationToken cancellationToken);
}

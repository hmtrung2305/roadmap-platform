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

public interface IMarketPulseAdminService
{
    Task<IReadOnlyList<MarketPulseCrawlRunDto>> GetCrawlRunsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPulseFailedItemDto>> GetFailedItemsAsync(
        MarketPulseAdminQueryDto query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPulseFailedItemDto>> RetryFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPulseFailedItemDto>> IgnoreFailedItemsAsync(
        IReadOnlyCollection<Guid> failedItemIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPulseClassifierMappingDto>> GetClassifierMappingsAsync(
        CancellationToken cancellationToken);

    Task<MarketPulseClassifierMappingDto> CreateClassifierMappingAsync(
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken);

    Task<MarketPulseClassifierMappingDto> UpdateClassifierMappingAsync(
        Guid mappingId,
        MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken);

    Task DeleteClassifierMappingAsync(Guid mappingId, CancellationToken cancellationToken);

    Task<MarketPulseClassifierTestResultDto> TestClassifierAsync(
        MarketPulseClassifierTestRequestDto request,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MarketPulseSourceHealthDto>> GetSourceHealthAsync(
        CancellationToken cancellationToken);
}

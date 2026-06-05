using RoadmapPlatform.Application.DTOs.AiCredits;

namespace RoadmapPlatform.Application.Interfaces.AiCredits
{
    public interface IAiCreditService
    {
        Task<AiCreditStatusDto> GetStatusAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<AiCreditStatusDto> EnsureCanSpendAsync(
            Guid userId,
            string featureName,
            int creditCost,
            CancellationToken cancellationToken = default);

        Task<AiCreditStatusDto> RecordUsageAsync(
            Guid userId,
            string featureName,
            int creditCost,
            Guid? requestRefId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default);
    }
}
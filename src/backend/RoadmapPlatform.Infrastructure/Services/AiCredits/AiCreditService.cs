using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.AiCredits;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.AiCredits
{
    public class AiCreditService : IAiCreditService
    {
        private const string DefaultPlanCode = "free";
        private const int DefaultDailyLimit = 5;

        private readonly ApplicationDbContext _dbContext;

        public AiCreditService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AiCreditStatusDto> GetStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await BuildStatusAsync(userId, cancellationToken);
        }

        public async Task<AiCreditStatusDto> EnsureCanSpendAsync(
            Guid userId,
            string featureName,
            int creditCost,
            CancellationToken cancellationToken = default)
        {
            ValidateCreditRequest(featureName, creditCost);

            var status = await BuildStatusAsync(userId, cancellationToken);
            if (status.RemainingCreditsToday < creditCost)
            {
                throw new AiCreditLimitExceededException(status);
            }

            return status;
        }

        public async Task<AiCreditStatusDto> RecordUsageAsync(
            Guid userId,
            string featureName,
            int creditCost,
            Guid? requestRefId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default)
        {
            ValidateCreditRequest(featureName, creditCost);

            _dbContext.AiCreditUsages.Add(new AiCreditUsage
            {
                UserId = userId,
                FeatureName = featureName.Trim(),
                CreditCost = creditCost,
                RequestRefId = requestRefId,
                Metadata = metadata
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await BuildStatusAsync(userId, cancellationToken);
        }

        private async Task<AiCreditStatusDto> BuildStatusAsync(
            Guid userId,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var dayStart = now.Date;
            var resetAt = dayStart.AddDays(1);

            var plan = await GetActivePlanAsync(userId, now, cancellationToken);

            var usedToday = await _dbContext.AiCreditUsages
                .AsNoTracking()
                .Where(usage =>
                    usage.UserId == userId &&
                    usage.CreatedAt >= dayStart &&
                    usage.CreatedAt < resetAt)
                .SumAsync(usage => (int?)usage.CreditCost, cancellationToken) ?? 0;

            var remaining = Math.Max(0, plan.DailyCreditLimit - usedToday);

            return new AiCreditStatusDto
            {
                PlanCode = plan.PlanCode,
                DailyCreditLimit = plan.DailyCreditLimit,
                UsedCreditsToday = usedToday,
                RemainingCreditsToday = remaining,
                ResetAt = new DateTimeOffset(resetAt, TimeSpan.Zero)
            };
        }

        private async Task<AiCreditPlan> GetActivePlanAsync(
            Guid userId,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var userPlan = await _dbContext.UserAiCreditPlans
                .AsNoTracking()
                .Include(userAiCreditPlan => userAiCreditPlan.PlanCodeNavigation)
                .FirstOrDefaultAsync(userAiCreditPlan =>
                    userAiCreditPlan.UserId == userId &&
                    (userAiCreditPlan.ExpiresAt == null || userAiCreditPlan.ExpiresAt > now),
                    cancellationToken);

            if (userPlan?.PlanCodeNavigation != null)
            {
                return userPlan.PlanCodeNavigation;
            }

            var defaultPlan = await _dbContext.AiCreditPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(plan => plan.PlanCode == DefaultPlanCode, cancellationToken);

            return defaultPlan ?? new AiCreditPlan
            {
                PlanCode = DefaultPlanCode,
                DailyCreditLimit = DefaultDailyLimit,
                Description = "Fallback free plan"
            };
        }

        private static void ValidateCreditRequest(string featureName, int creditCost)
        {
            if (string.IsNullOrWhiteSpace(featureName))
            {
                throw new ArgumentException("Feature name is required.", nameof(featureName));
            }

            if (creditCost <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(creditCost), "Credit cost must be greater than zero.");
            }
        }
    }
}
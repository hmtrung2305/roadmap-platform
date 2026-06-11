using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoadmapPlatform.Infrastructure.Services.GitHub
{
    public class RepoInsightService : IRepoInsightService
    {
        private const string RepoInsightFeatureName = "repo_insight";
        private const int RepoInsightCreditCost = 1;
        private const int MaxReadmeCharacters = 12000;
        private const string CompletedStatus = "completed";
        private const string FailedStatus = "failed";

        private readonly ApplicationDbContext _dbContext;
        private readonly IGitHubApiClient _gitHubApiClient;
        private readonly IRepoSummaryGenerator _repoSummaryGenerator;
        private readonly IAiCreditService _aiCreditService;

        public RepoInsightService(
            ApplicationDbContext dbContext,
            IGitHubApiClient gitHubApiClient,
            IRepoSummaryGenerator repoSummaryGenerator,
            IAiCreditService aiCreditService)
        {
            _dbContext = dbContext;
            _gitHubApiClient = gitHubApiClient;
            _repoSummaryGenerator = repoSummaryGenerator;
            _aiCreditService = aiCreditService;
        }

        public async Task<RepoInsightResponseDto> GenerateInsightAsync(
            Guid userId,
            Guid repositoryId,
            bool force = false,
            CancellationToken cancellationToken = default)
        {
            var repository = await _dbContext.Repositories
                .Include(repo => repo.RepoInsight)
                .FirstOrDefaultAsync(repo =>
                    repo.RepositoryId == repositoryId &&
                    repo.UserId == userId,
                    cancellationToken);

            if (repository == null)
            {
                throw new NotFoundException("Repository was not found");
            }

            var existingInsight = repository.RepoInsight;
            var (owner, repoName) = ParseRepositoryFullName(repository.FullName);

            var readme = await _gitHubApiClient.GetRepositoryReadmeAsync(owner, repoName, cancellationToken);

            if (string.IsNullOrWhiteSpace(readme))
            {
                var failedInsight = UpsertFailedInsight(
                    repository,
                    existingInsight,
                    "README was not found for this repository.");

                await _dbContext.SaveChangesAsync(cancellationToken);
                return ToDto(failedInsight);
            }

            var cleanedReadme = CleanReadme(readme);
            var readmeHash = ComputeSha256Hash(cleanedReadme);

            if (!force &&
                existingInsight != null &&
                existingInsight.AnalysisStatus == CompletedStatus &&
                existingInsight.ReadmeHash == readmeHash)
            {
                return ToDto(existingInsight);
            }

            var (limitedReadme, readmeTruncated) = LimitReadme(cleanedReadme);

            await _aiCreditService.EnsureCanSpendAsync(
                userId,
                RepoInsightFeatureName,
                RepoInsightCreditCost,
                cancellationToken);

            GeneratedRepoInsightDto generatedInsight;

            try
            {
                generatedInsight = await _repoSummaryGenerator.GenerateAsync(
                    new RepoSummaryGenerationRequestDto
                    {
                        Name = repository.Name,
                        FullName = repository.FullName,
                        Description = repository.Description,
                        PrimaryLanguage = repository.PrimaryLanguage,
                        Stars = repository.Stars,
                        Forks = repository.Forks,
                        Readme = limitedReadme
                    },
                    cancellationToken);
            }
            catch (Exception ex)
            {
                var failedInsight = UpsertFailedInsight(
                    repository,
                    existingInsight,
                    "Repository AI summary generation failed: " + ex.Message,
                    readmeHash,
                    readmeTruncated);

                await _dbContext.SaveChangesAsync(cancellationToken);
                return ToDto(failedInsight);
            }

            var insight = UpsertCompletedInsight(
                repository,
                existingInsight,
                generatedInsight,
                readmeHash,
                readmeTruncated);

            await _dbContext.SaveChangesAsync(cancellationToken);

            await _aiCreditService.RecordUsageAsync(
                userId,
                RepoInsightFeatureName,
                RepoInsightCreditCost,
                insight.InsightId,
                cancellationToken: cancellationToken);

            return ToDto(insight);
        }

        private static RepoInsight UpsertCompletedInsight(
            Repository repository,
            RepoInsight? existingInsight,
            GeneratedRepoInsightDto generatedInsight,
            string readmeHash,
            bool readmeTruncated)
        {
            var now = DateTime.UtcNow;
            var insight = existingInsight ?? new RepoInsight
            {
                RepositoryId = repository.RepositoryId,
                Repository = repository
            };

            insight.Summary = generatedInsight.Summary;
            insight.TechStack = SerializeStringList(generatedInsight.TechStack);
            insight.DetectedSkills = SerializeStringList(generatedInsight.DetectedSkills);
            insight.ProjectType = generatedInsight.ProjectType;
            insight.AnalysisStatus = CompletedStatus;
            insight.ReadmeHash = readmeHash;
            insight.ReadmeTruncated = readmeTruncated;
            insight.AiModel = generatedInsight.AiModel;
            insight.ErrorMessage = null;
            insight.AnalyzedAt = now;
            insight.UpdatedAt = now;

            if (existingInsight == null)
            {
                repository.RepoInsight = insight;
            }

            return insight;
        }

        private static RepoInsight UpsertFailedInsight(
            Repository repository,
            RepoInsight? existingInsight,
            string errorMessage,
            string? readmeHash = null,
            bool readmeTruncated = false)
        {
            var now = DateTime.UtcNow;

            if (existingInsight?.AnalysisStatus == CompletedStatus)
            {
                existingInsight.ErrorMessage = $"Latest refresh failed: {errorMessage}";
                existingInsight.UpdatedAt = now;
                return existingInsight;
            }

            var insight = existingInsight ?? new RepoInsight
            {
                RepositoryId = repository.RepositoryId,
                Repository = repository
            };

            insight.AnalysisStatus = FailedStatus;
            insight.ReadmeHash = readmeHash;
            insight.ReadmeTruncated = readmeTruncated;
            insight.ErrorMessage = errorMessage;
            insight.UpdatedAt = now;

            if (existingInsight == null)
            {
                insight.AnalyzedAt = now;
                repository.RepoInsight = insight;
            }

            return insight;
        }

        private static (string Owner, string RepoName) ParseRepositoryFullName(string fullName)
        {
            var parts = fullName.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
            {
                throw new InvalidOperationException("Repository full name must use the owner/repository format.");
            }

            return (parts[0], parts[1]);
        }

        private static string CleanReadme(string readme)
        {
            var cleaned = Regex.Replace(readme, @"<!--.*?-->", string.Empty, RegexOptions.Singleline);
            cleaned = Regex.Replace(cleaned, @"!\[[^\]]*\]\([^\)]*\)", string.Empty);
            cleaned = Regex.Replace(cleaned, @"\[!\[[^\]]*\]\([^\)]*\)\]\([^\)]*\)", string.Empty);
            cleaned = Regex.Replace(cleaned, @"<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");

            return cleaned.Trim();
        }

        private static (string Readme, bool Truncated) LimitReadme(string readme)
        {
            if (readme.Length <= MaxReadmeCharacters)
            {
                return (readme, false);
            }

            return (readme[..MaxReadmeCharacters], true);
        }

        private static string ComputeSha256Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string SerializeStringList(List<string> values)
        {
            var normalized = values
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return JsonSerializer.Serialize(normalized);
        }

        public static RepoInsightResponseDto ToDto(RepoInsight insight)
        {
            return new RepoInsightResponseDto
            {
                InsightId = insight.InsightId,
                RepositoryId = insight.RepositoryId,
                Summary = insight.Summary,
                TechStack = DeserializeStringList(insight.TechStack),
                DetectedSkills = DeserializeStringList(insight.DetectedSkills),
                ProjectType = insight.ProjectType,
                AnalysisStatus = insight.AnalysisStatus,
                ReadmeTruncated = insight.ReadmeTruncated,
                AiModel = insight.AiModel,
                ErrorMessage = insight.ErrorMessage,
                AnalyzedAt = insight.AnalyzedAt,
                UpdatedAt = insight.UpdatedAt
            };
        }

        public static List<string> DeserializeStringList(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (JsonException)
            {
                return new List<string>();
            }
        }
    }
}

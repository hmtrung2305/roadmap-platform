using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.AiCredits;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Models.GitHub;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.GitHub;

namespace RoadmapPlatform.Tests;

public sealed class RepositoryInsightTests
{
    [Fact]
    public async Task TC055_GenerateInsight_WithEligibleReadme_SavesCompletedInsightAndSpendsOneCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await CreateScenarioAsync(db, ValidReadme());

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId);

        Assert.Equal("completed", result.AnalysisStatus);
        Assert.Equal("A tested repository summary", result.Summary);
        Assert.Equal(1, scenario.CreditService.SpendCallCount);
        Assert.Equal(1, scenario.Generator.CallCount);
        var saved = await db.RepoInsights.SingleAsync();
        Assert.Equal("completed", saved.AnalysisStatus);
    }

    [Fact]
    public async Task TC056_GenerateInsight_With399MeaningfulCharacters_RejectsWithoutSpendingCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await CreateScenarioAsync(db, new string('a', 399));

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId);

        Assert.Equal("failed", result.AnalysisStatus);
        Assert.Contains("README does not contain enough meaningful information", result.ErrorMessage);
        Assert.Equal(0, scenario.CreditService.SpendCallCount);
        Assert.Equal(0, scenario.Generator.CallCount);
    }

    [Fact]
    public async Task TC057_GenerateInsight_WithFewerThan70Words_RejectsWithoutSpendingCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var words = string.Join(' ', Enumerable.Repeat("abcdefghij", 60));
        var readme = $"# Overview\n{words}\n## Features\n```text\nexample\n```";
        var scenario = await CreateScenarioAsync(db, readme);

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId);

        Assert.Equal("failed", result.AnalysisStatus);
        Assert.Equal(0, scenario.CreditService.SpendCallCount);
        Assert.Equal(0, scenario.Generator.CallCount);
    }

    [Fact]
    public async Task TC058_GenerateInsight_WithOnlyOneOrNoEvidenceSignal_RejectsWithoutSpendingCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var readme = string.Join(' ', Enumerable.Repeat("meaningfulprojectword", 80));
        var scenario = await CreateScenarioAsync(db, readme);

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId);

        Assert.Equal("failed", result.AnalysisStatus);
        Assert.Equal(0, scenario.CreditService.SpendCallCount);
        Assert.Equal(0, scenario.Generator.CallCount);
    }

    [Fact]
    public async Task TC059_GenerateInsight_WithOversizedReadme_LimitsProviderPayloadTo12000Characters()
    {
        await using var db = TestDbContextFactory.Create();
        var readme = ValidReadme() + new string('x', 13_000);
        var scenario = await CreateScenarioAsync(db, readme);

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId);

        Assert.True(result.ReadmeTruncated);
        Assert.NotNull(scenario.Generator.LastRequest);
        Assert.Equal(12_000, scenario.Generator.LastRequest!.Readme.Length);
    }

    [Fact]
    public async Task TC060_GenerateInsight_WithUnchangedReadme_ReturnsCacheWithoutSpendingCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var readme = ValidReadme();
        var hash = ComputeHash(readme.Trim());
        var scenario = await CreateScenarioAsync(db, readme, hash, "Cached summary");

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId, force: false);

        Assert.Equal("Cached summary", result.Summary);
        Assert.Equal(0, scenario.CreditService.SpendCallCount);
        Assert.Equal(0, scenario.Generator.CallCount);
    }

    [Fact]
    public async Task TC061_GenerateInsight_WithForceTrue_RegeneratesCacheAndSpendsOneCredit()
    {
        await using var db = TestDbContextFactory.Create();
        var readme = ValidReadme();
        var scenario = await CreateScenarioAsync(db, readme, ComputeHash(readme.Trim()), "Old summary");
        scenario.Generator.Result.Summary = "Regenerated summary";

        var result = await scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId, force: true);

        Assert.Equal("Regenerated summary", result.Summary);
        Assert.Equal(1, scenario.CreditService.SpendCallCount);
        Assert.Equal(1, scenario.Generator.CallCount);
    }

    [Fact]
    public async Task TC062_GenerateInsight_WithZeroCredits_ThrowsBeforeProviderCall()
    {
        await using var db = TestDbContextFactory.Create();
        var scenario = await CreateScenarioAsync(db, ValidReadme());
        scenario.CreditService.ExceptionToThrow = new AiCreditLimitExceededException(new AiCreditStatusDto
        {
            DailyCreditLimit = 0,
            RemainingCreditsToday = 0,
        });

        await Assert.ThrowsAsync<AiCreditLimitExceededException>(() =>
            scenario.Service.GenerateInsightAsync(scenario.UserId, scenario.RepositoryId));

        Assert.Equal(1, scenario.CreditService.SpendCallCount);
        Assert.Equal(0, scenario.Generator.CallCount);
        Assert.Empty(db.RepoInsights);
    }

    private static async Task<RepoScenario> CreateScenarioAsync(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext db,
        string readme,
        string? cachedHash = null,
        string? cachedSummary = null)
    {
        var user = TestEntityFactory.CreateUser("repo-user", email: "repo@example.com");
        var repository = new Repository
        {
            RepositoryId = Guid.NewGuid(),
            UserId = user.UserId,
            User = user,
            GithubRepoId = 101,
            Name = "roadmap-platform",
            FullName = "team/roadmap-platform",
            HtmlUrl = "https://github.com/team/roadmap-platform",
            Description = "Learning roadmap platform",
            PrimaryLanguage = "C#",
            Stars = 10,
            Forks = 2,
            IsPrivate = false,
            IsSelectedForPortfolio = false,
            SyncedAt = DateTime.UtcNow,
        };
        user.Repositories.Add(repository);

        if (cachedHash is not null)
        {
            var cached = new RepoInsight
            {
                InsightId = Guid.NewGuid(),
                RepositoryId = repository.RepositoryId,
                Repository = repository,
                Summary = cachedSummary,
                TechStack = "[]",
                DetectedSkills = "[]",
                ProjectType = "Web",
                AnalysisStatus = "completed",
                ReadmeHash = cachedHash,
                ReadmeTruncated = false,
                AiModel = "cached-model",
                AnalyzedAt = DateTime.UtcNow.AddMinutes(-10),
                UpdatedAt = DateTime.UtcNow.AddMinutes(-10),
            };
            repository.RepoInsight = cached;
        }

        db.Add(user);
        await db.SaveChangesAsync();

        var api = new FakeGitHubApiClient { Readme = readme };
        var token = new FakeGitHubTokenService();
        var generator = new FakeRepoSummaryGenerator();
        var credits = new FakeAiCreditService();
        var service = new RepoInsightService(db, api, token, generator, credits);

        return new RepoScenario(user.UserId, repository.RepositoryId, service, generator, credits);
    }

    private static string ValidReadme()
    {
        var prose = string.Join(' ', Enumerable.Repeat(
            "This platform helps learners build practical software skills through structured roadmaps and measurable progress", 12));
        return $"# Overview\n{prose}\n## Features\n- Create learning roadmaps\n- Track learner progress\n- Analyze skill gaps\n## Tech Stack\nASP.NET React PostgreSQL Docker";
    }

    private static string ComputeHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    }

    private sealed record RepoScenario(
        Guid UserId,
        Guid RepositoryId,
        RepoInsightService Service,
        FakeRepoSummaryGenerator Generator,
        FakeAiCreditService CreditService);

    private sealed class FakeGitHubApiClient : IGitHubApiClient
    {
        public string? Readme { get; set; }

        public Task<List<GitHubRepositorySyncDto>> GetPublicRepositoriesAsync(
            string username,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new List<GitHubRepositorySyncDto>());
        }

        public Task<string?> GetRepositoryReadmeAsync(
            string owner,
            string repositoryName,
            string accessToken,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Readme);
        }
    }

    private sealed class FakeGitHubTokenService : IGitHubTokenService
    {
        public Task<GitHubAccessTokenContext> GetRequiredAccessTokenAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GitHubAccessTokenContext
            {
                Username = "team",
                AccessToken = "token",
            });
        }
    }

    private sealed class FakeRepoSummaryGenerator : IRepoSummaryGenerator
    {
        public int CallCount { get; private set; }
        public RepoSummaryGenerationRequestDto? LastRequest { get; private set; }
        public GeneratedRepoInsightDto Result { get; } = new()
        {
            HasSufficientEvidence = true,
            Summary = "A tested repository summary",
            TechStack = ["ASP.NET Core", "React"],
            DetectedSkills = ["C#", "Testing"],
            ProjectType = "Web Application",
            AiModel = "test-model",
        };

        public Task<GeneratedRepoInsightDto> GenerateAsync(
            RepoSummaryGenerationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            return Task.FromResult(Result);
        }
    }

    private sealed class FakeAiCreditService : IAiCreditService
    {
        public int SpendCallCount { get; private set; }
        public Exception? ExceptionToThrow { get; set; }

        public Task<AiCreditStatusDto> GetStatusAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AiCreditStatusDto { RemainingCreditsToday = 10 });
        }

        public Task<AiCreditStatusDto> SpendAsync(
            Guid userId,
            string featureName,
            int creditCost,
            Guid? requestRefId = null,
            string? metadata = null,
            CancellationToken cancellationToken = default)
        {
            SpendCallCount++;
            if (ExceptionToThrow is not null)
            {
                return Task.FromException<AiCreditStatusDto>(ExceptionToThrow);
            }

            return Task.FromResult(new AiCreditStatusDto { RemainingCreditsToday = 9 });
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.GitHub;
using RoadmapPlatform.Application.DTOs.AiMentor;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.GitHub;
using RoadmapPlatform.Application.Models.GitHub;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.AiCredits;
using RoadmapPlatform.Infrastructure.Services.AiMentor;
using RoadmapPlatform.Infrastructure.Services.GitHub;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.AI.Credits;

public sealed class AiCreditTests
{
    [Fact]
    [Trait("TestCaseId", "TC161")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "Integration")]
    public async Task TC161_GetCreditStatus_ShouldReturnExistingBalance()
    {
        var userId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var today = DateTime.UtcNow.Date;

        context.AiCreditPlans.Add(new AiCreditPlan
        {
            PlanCode = "free",
            DailyCreditLimit = 10,
            CreatedAt = today
        });
        context.UserAiCreditPlans.Add(new UserAiCreditPlan
        {
            UserId = userId,
            PlanCode = "free",
            CreatedAt = today,
            UpdatedAt = today
        });
        context.AiCreditUsages.Add(new AiCreditUsage
        {
            UsageId = Guid.NewGuid(),
            UserId = userId,
            FeatureName = "ai_mentor_chat",
            CreditCost = 3,
            CreatedAt = today
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await new AiCreditService(context).GetStatusAsync(userId);

        Assert.Equal(7, result.RemainingCreditsToday);
    }

    [Fact]
    [Trait("TestCaseId", "TC162")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "SourceContract")]
    public void TC162_SuccessfulMentorResponse_ShouldSpendExactlyOneCreditAndStoreAnswer()
    {
        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "AiMentor", "AiMentorService.cs");

        Assert.Contains("private const int AiMentorCreditCost = 1;", source, StringComparison.Ordinal);

        var spendIndex = source.IndexOf("await _aiCreditService.SpendAsync(", StringComparison.Ordinal);
        var generationIndex = source.IndexOf("await GenerateAssistantAnswerAsync(", StringComparison.Ordinal);
        var userMessageIndex = source.IndexOf("_context.AiMentorMessages.Add(userMessage);", StringComparison.Ordinal);
        var assistantMessageIndex = source.IndexOf("_context.AiMentorMessages.Add(assistantMessage);", StringComparison.Ordinal);
        var saveIndex = source.IndexOf("await _context.SaveChangesAsync(cancellationToken);", assistantMessageIndex, StringComparison.Ordinal);

        Assert.True(spendIndex >= 0);
        Assert.True(generationIndex > spendIndex);
        Assert.True(userMessageIndex > generationIndex);
        Assert.True(assistantMessageIndex > userMessageIndex);
        Assert.True(saveIndex > assistantMessageIndex);
    }

    [Fact]
    [Trait("TestCaseId", "TC163")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "Integration")]
    public async Task TC163_InvalidMentorRequest_ShouldNotSpendCredit()
    {
        await using var context = Tester4TestSupport.CreateContext();
        var credits = new RecordingAiCreditService { RemainingCredits = 3 };
        var service = CreateMentorService(context, credits);

        await Assert.ThrowsAsync<ConflictException>(() => service.AskAsync(
            Guid.NewGuid(),
            new AiMentorChatRequestDto { Message = "   " },
            CancellationToken.None));

        Assert.Equal(0, credits.SpendCalls);
        Assert.Empty(context.AiMentorMessages);
    }

    [Fact]
    [Trait("TestCaseId", "TC164")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "Integration")]
    public async Task TC164_ZeroCredits_ShouldBlockMentorBeforeAssistantMessageIsGenerated()
    {
        await using var context = Tester4TestSupport.CreateContext();
        var credits = new RecordingAiCreditService
        {
            RemainingCredits = 0,
            ThrowLimitExceeded = true
        };
        var service = CreateMentorService(context, credits);

        await Assert.ThrowsAsync<AiCreditLimitExceededException>(() => service.AskAsync(
            Guid.NewGuid(),
            new AiMentorChatRequestDto { Message = "How should I learn backend development?" },
            CancellationToken.None));

        Assert.Equal(1, credits.SpendCalls);
        Assert.Empty(context.AiMentorMessages);
        Assert.Equal(0, await context.AiMentorConversations.CountAsync());
    }

    [Fact]
    [Trait("TestCaseId", "TC171")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "SourceContract")]
    public void TC171_SuccessfulModuleAssistantResponse_ShouldSpendExactlyOneCredit()
    {
        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "LearningModules", "LearningModuleChatService.cs");

        Assert.Contains("private const int LearningModuleChatCreditCost = 1;", source, StringComparison.Ordinal);

        var noSourceReturnIndex = source.IndexOf("if (ragSources.Count == 0)", StringComparison.Ordinal);
        var spendIndex = source.IndexOf("await _aiCreditService.SpendAsync(", StringComparison.Ordinal);
        var generateIndex = source.IndexOf("await GenerateAnswerAsync(", StringComparison.Ordinal);

        Assert.True(noSourceReturnIndex >= 0);
        Assert.True(spendIndex > noSourceReturnIndex);
        Assert.True(generateIndex > spendIndex);
    }

    [Fact]
    [Trait("TestCaseId", "TC172")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Credits")]
    [Trait("TestType", "Integration")]
    public async Task TC172_SuccessfulRepositoryInsight_ShouldSpendExactlyOneCredit()
    {
        var userId = Guid.NewGuid();
        var repositoryId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();

        context.Repositories.Add(new Repository
        {
            RepositoryId = repositoryId,
            UserId = userId,
            GithubRepoId = 123,
            Name = "roadmap-platform",
            FullName = "tester/roadmap-platform",
            HtmlUrl = "https://example.test/tester/roadmap-platform",
            Description = "A platform for roadmap learning and portfolio development.",
            PrimaryLanguage = "C#",
            Stars = 10,
            Forks = 2,
            IsPrivate = false,
            SyncedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var credits = new RecordingAiCreditService { RemainingCredits = 4 };
        var service = new RepoInsightService(
            context,
            new StubGitHubApiClient(CreateEligibleReadme()),
            new StubGitHubTokenService(),
            new StubRepoSummaryGenerator(),
            credits);

        var result = await service.GenerateInsightAsync(userId, repositoryId);

        Assert.Equal("completed", result.AnalysisStatus);
        Assert.Equal(1, credits.SpendCalls);
        Assert.Equal("repo_insight", credits.LastFeatureName);
        Assert.Equal(1, credits.LastCreditCost);
        Assert.Equal(repositoryId, credits.LastRequestRefId);
        Assert.Equal(3, credits.RemainingCredits);
    }

    private static AiMentorService CreateMentorService(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext context,
        RecordingAiCreditService credits)
    {
        return new AiMentorService(
            context,
            credits,
            Options.Create(new AiSettings
            {
                ApiKey = "tester4-placeholder-key",
                GenerationModel = "gemini-2.5-flash"
            }));
    }

    private static string CreateEligibleReadme()
    {
        return """
            # Roadmap Platform

            Roadmap Platform is a web application that helps learners follow
            structured career roadmaps and complete learning modules. The system
            supports learner enrollment, lesson progress, quizzes, AI mentor
            conversations, GitHub repository insights, portfolio publishing,
            skill-gap analysis, and role-based administration.

            ## Features

            - Learners can enroll in published modules and track lesson progress.
            - Content managers can create lessons, quizzes, and publish modules.
            - Users can connect GitHub repositories and generate AI project insights.
            - AI Mentor provides contextual guidance based on available learning data.
            - Administrators manage users, roles, permissions, and system content.

            ## Technology Stack

            The backend uses ASP.NET Core, Entity Framework Core, PostgreSQL,
            pgvector, authentication, authorization, and REST APIs. The frontend
            uses React, Vite, Tailwind CSS, and reusable UI components. The project
            also contains automated xUnit tests, database migrations, validation,
            API documentation, and background workers for lesson indexing.

            ## Usage

            Learners sign in, select a roadmap, enroll in learning modules, open
            lessons, complete quizzes, and update their progress. Content managers
            prepare learning content and publish validated modules for learners.
            """;
    }

    private sealed class StubGitHubApiClient(string readme) : IGitHubApiClient
    {
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
            return Task.FromResult<string?>(readme);
        }
    }

    private sealed class StubGitHubTokenService : IGitHubTokenService
    {
        public Task<GitHubAccessTokenContext> GetRequiredAccessTokenAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GitHubAccessTokenContext
            {
                Username = "tester",
                AccessToken = "test-token"
            });
        }
    }

    private sealed class StubRepoSummaryGenerator : IRepoSummaryGenerator
    {
        public Task<GeneratedRepoInsightDto> GenerateAsync(
            RepoSummaryGenerationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GeneratedRepoInsightDto
            {
                HasSufficientEvidence = true,
                PurposeEvidence = "README describes the learning platform.",
                Summary = "A roadmap-based learning platform.",
                TechStack = ["ASP.NET Core", "React", "PostgreSQL"],
                DetectedSkills = ["C#", "React"],
                ProjectType = "Web Application",
                AiModel = "fake-model"
            });
        }
    }
}

using System.Reflection;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Services.LearningModules;

namespace RoadmapPlatform.Tests.LearningModules.Progress;

public sealed class ModuleAssistantTests
{
    [Fact]
    public async Task TC224_AskAsync_WhenQuestionTargetsCurrentLesson_ShouldPreferCurrentLessonContextAndSpendOneCredit()
    {
        await using var fixture = await ModuleAssistantTestFixture.CreateAsync();

        var sources = await InvokeCurrentLessonSourceRetrievalAsync(fixture);

        var exception = await Assert.ThrowsAsync<
            ModuleAssistantTestFixture.CreditSpendInterceptedException>(() =>
                fixture.Service.AskAsync(
                    fixture.UserId,
                    fixture.ModuleId,
                    new LearningModuleChatRequestDto
                    {
                        SkillModuleLessonId = fixture.CurrentLessonId,
                        Message = "What is this lesson about?"
                    },
                    CancellationToken.None));

        Assert.NotNull(exception);
        var source = Assert.Single(sources);
        Assert.Equal(fixture.CurrentLessonChunkId, source.SkillModuleChunkId);
        Assert.Equal(fixture.CurrentLessonId, source.SkillModuleLessonId);
        Assert.Equal("Dependency Injection Basics", source.LessonTitle);
        Assert.Contains(fixture.CurrentLessonContent, source.ContentPreview);

        Assert.Equal(0, fixture.RagIndexingService.SearchCallCount);
        Assert.Equal(1, fixture.AiCreditService.SpendCallCount);
        Assert.Equal(4, fixture.AiCreditService.RemainingCredits);
        Assert.Equal(fixture.UserId, fixture.AiCreditService.LastUserId);
        Assert.Equal("learning_module_chat", fixture.AiCreditService.LastFeatureName);
        Assert.Equal(1, fixture.AiCreditService.LastCreditCost);
        Assert.Equal(fixture.ModuleId, fixture.AiCreditService.LastRequestRefId);
    }

    private static async Task<IReadOnlyList<LearningModuleRagSourceDto>>
        InvokeCurrentLessonSourceRetrievalAsync(
            ModuleAssistantTestFixture fixture)
    {
        var method = typeof(LearningModuleChatService).GetMethod(
            "GetCurrentLessonSourcesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (method is null)
        {
            throw new MissingMethodException(
                nameof(LearningModuleChatService),
                "GetCurrentLessonSourcesAsync");
        }

        var task = method.Invoke(
            fixture.Service,
            new object[]
            {
                fixture.CurrentLesson,
                CancellationToken.None
            }) as Task<IReadOnlyList<LearningModuleRagSourceDto>>;

        if (task is null)
        {
            throw new InvalidOperationException(
                "Current lesson source retrieval did not return the expected task.");
        }

        return await task;
    }

    [Fact]
    public async Task TC225_AskAsync_WhenCurrentLessonHasNoMatch_ShouldUseModuleContextAndSpendOneCredit()
    {
        await using var fixture = await ModuleAssistantTestFixture.CreateAsync();
        var moduleLevelSource = fixture.CreateOtherLessonSource();
        fixture.RagIndexingService.SearchResults = [moduleLevelSource];

        var exception = await Assert.ThrowsAsync<
            ModuleAssistantTestFixture.CreditSpendInterceptedException>(() =>
                fixture.Service.AskAsync(
                    fixture.UserId,
                    fixture.ModuleId,
                    new LearningModuleChatRequestDto
                    {
                        SkillModuleLessonId = fixture.CurrentLessonId,
                        Message = "How does constructor injection improve testability?"
                    },
                    CancellationToken.None));

        Assert.NotNull(exception);
        Assert.Equal(1, fixture.RagIndexingService.SearchCallCount);
        Assert.Equal(fixture.ModuleId, fixture.RagIndexingService.LastSkillModuleId);
        Assert.Equal(
            fixture.CurrentLessonId,
            fixture.RagIndexingService.LastPreferredLessonId);
        Assert.NotNull(fixture.RagIndexingService.LastQuery);
        Assert.Contains(
            "How does constructor injection improve testability?",
            fixture.RagIndexingService.LastQuery!);

        Assert.Equal(fixture.OtherLessonChunkId, moduleLevelSource.SkillModuleChunkId);
        Assert.Equal(fixture.OtherLessonId, moduleLevelSource.SkillModuleLessonId);
        Assert.Equal("Constructor Injection", moduleLevelSource.LessonTitle);

        Assert.Equal(1, fixture.AiCreditService.SpendCallCount);
        Assert.Equal(4, fixture.AiCreditService.RemainingCredits);
        Assert.Equal(fixture.UserId, fixture.AiCreditService.LastUserId);
        Assert.Equal("learning_module_chat", fixture.AiCreditService.LastFeatureName);
        Assert.Equal(1, fixture.AiCreditService.LastCreditCost);
        Assert.Equal(fixture.ModuleId, fixture.AiCreditService.LastRequestRefId);
    }

    [Fact]
    public async Task TC226_AskAsync_WhenNoLessonContextIsFound_ShouldReturnNoContextResponseWithoutSpendingCredit()
    {
        await using var fixture = await ModuleAssistantTestFixture.CreateAsync();
        fixture.RagIndexingService.SearchResults = [];
        var creditsBefore = fixture.AiCreditService.RemainingCredits;

        var response = await fixture.Service.AskAsync(
            fixture.UserId,
            fixture.ModuleId,
            new LearningModuleChatRequestDto
            {
                SkillModuleLessonId = fixture.CurrentLessonId,
                Message = "What is the current weather on Mars?"
            },
            CancellationToken.None);

        Assert.Equal(
            "I could not find anything in this module's lesson content that answers that question.",
            response.Answer);
        Assert.Empty(response.Sources);
        Assert.Equal(1, fixture.RagIndexingService.SearchCallCount);
        Assert.Equal(0, fixture.AiCreditService.SpendCallCount);
        Assert.Equal(creditsBefore, fixture.AiCreditService.RemainingCredits);
    }

    [Fact]
    public async Task TC227_AskAsync_WhenLearnerIsNotEnrolled_ShouldThrowConflictWithoutRetrievalOrCreditSpend()
    {
        await using var fixture = await ModuleAssistantTestFixture.CreateAsync(
            enrolled: false);
        var creditsBefore = fixture.AiCreditService.RemainingCredits;

        var exception = await Record.ExceptionAsync(() =>
            fixture.Service.AskAsync(
                fixture.UserId,
                fixture.ModuleId,
                new LearningModuleChatRequestDto
                {
                    SkillModuleLessonId = fixture.CurrentLessonId,
                    Message = "What is this lesson about?"
                },
                CancellationToken.None));

        Assert.NotNull(exception);
        Assert.Equal(0, fixture.RagIndexingService.SearchCallCount);
        Assert.Equal(0, fixture.AiCreditService.SpendCallCount);
        Assert.Equal(creditsBefore, fixture.AiCreditService.RemainingCredits);

        var conflictException = Assert.IsType<ConflictException>(exception);
        Assert.Equal(
            "Start the module before using module chat.",
            conflictException.Message);
    }
}

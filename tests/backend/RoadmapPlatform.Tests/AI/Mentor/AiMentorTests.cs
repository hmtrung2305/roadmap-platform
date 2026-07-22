using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.AiMentor;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Entities;
using RoadmapPlatform.Infrastructure.Services.AiMentor;
using RoadmapPlatform.Tests.TestInfrastructure;

namespace RoadmapPlatform.Tests.AI.Mentor;

public sealed class AiMentorTests
{
    [Fact]
    [Trait("TestCaseId", "TC165")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Mentor")]
    [Trait("TestType", "Integration")]
    public async Task TC165_NewConversation_ShouldCreateConversationAndDefineTwoMessagePersistence()
    {
        var userId = Guid.NewGuid();
        await using var context = Tester4TestSupport.CreateContext();
        var service = CreateService(context);

        var conversation = await Tester4TestSupport.InvokePrivateAsync<AiMentorConversation>(
            service,
            "GetOrCreateConversationAsync",
            userId,
            null,
            "roadmap_selection",
            "How should I become a backend developer?",
            CancellationToken.None);

        Assert.Equal(userId, conversation.UserId);
        Assert.Equal("roadmap_selection", conversation.PageContext);
        Assert.Contains("How should I become", conversation.Title, StringComparison.Ordinal);
        Assert.Single(context.AiMentorConversations.Local);

        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "AiMentor", "AiMentorService.cs");
        Assert.Contains("_context.AiMentorMessages.Add(userMessage);", source, StringComparison.Ordinal);
        Assert.Contains("_context.AiMentorMessages.Add(assistantMessage);", source, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("TestCaseId", "TC166")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Mentor")]
    [Trait("TestType", "Integration")]
    public async Task TC166_ExistingConversation_ShouldReuseConversationAndLoadRecentContextInOrder()
    {
        var userId = Guid.NewGuid();
        var conversationId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddMinutes(-20);
        await using var context = Tester4TestSupport.CreateContext();

        context.AiMentorConversations.Add(new AiMentorConversation
        {
            AiMentorConversationId = conversationId,
            UserId = userId,
            Title = "Backend plan",
            PageContext = "roadmap_selection",
            CreatedAt = startedAt,
            UpdatedAt = startedAt
        });

        for (var index = 0; index < 12; index++)
        {
            context.AiMentorMessages.Add(new AiMentorMessage
            {
                AiMentorMessageId = Guid.NewGuid(),
                AiMentorConversationId = conversationId,
                Role = index % 2 == 0 ? "user" : "assistant",
                Content = $"Message {index}",
                Sources = "[]",
                CreatedAt = startedAt.AddMinutes(index)
            });
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var service = CreateService(context);
        var reused = await Tester4TestSupport.InvokePrivateAsync<AiMentorConversation>(
            service,
            "GetOrCreateConversationAsync",
            userId,
            conversationId,
            "ignored",
            "Follow-up question",
            CancellationToken.None);
        var recent = await Tester4TestSupport.InvokePrivateAsync<IReadOnlyList<AiMentorMessage>>(
            service,
            "LoadRecentMessagesAsync",
            conversationId,
            CancellationToken.None);

        Assert.Equal(conversationId, reused.AiMentorConversationId);
        Assert.Equal(10, recent.Count);
        Assert.Equal("Message 2", recent[0].Content);
        Assert.Equal("Message 11", recent[^1].Content);
        Assert.True(recent.Zip(recent.Skip(1), (first, second) => first.CreatedAt <= second.CreatedAt).All(value => value));
    }

    [Fact]
    [Trait("TestCaseId", "TC167")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Mentor")]
    [Trait("TestType", "SourceContract")]
    public void TC167_MentorContext_ShouldUsePermittedUserScopedDataAndPublishedRoadmaps()
    {
        var source = Tester4TestSupport.ReadRepositoryFile(
            "src", "backend", "RoadmapPlatform.Infrastructure", "Services", "AiMentor", "AiMentorService.cs");

        Assert.Contains("private const int RepositoryInsightLimit = 5;", source, StringComparison.Ordinal);
        Assert.Contains("private const int SkillGapHistoryLimit = 3;", source, StringComparison.Ordinal);
        Assert.Contains("repository.UserId == userId", source, StringComparison.Ordinal);
        Assert.Contains("history.UserId == userId", source, StringComparison.Ordinal);
        Assert.Contains("version.Status == \"published\"", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IgnoreQueryFilters", source, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("TestCaseId", "TC170")]
    [Trait("Owner", "Tester4")]
    [Trait("Module", "AI Mentor")]
    [Trait("TestType", "SecurityIntegration")]
    public async Task TC170_ForeignConversation_ShouldNotDiscloseMessages()
    {
        var ownerId = Guid.NewGuid();
        var attackerId = Guid.NewGuid();
        var conversation = CreateConversation(ownerId, "Private", DateTime.UtcNow, archivedAt: null);
        await using var context = Tester4TestSupport.CreateContext();
        context.AiMentorConversations.Add(conversation);
        context.AiMentorMessages.Add(new AiMentorMessage
        {
            AiMentorMessageId = Guid.NewGuid(),
            AiMentorConversationId = conversation.AiMentorConversationId,
            Role = "assistant",
            Content = "Private mentor response",
            Sources = "[]",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetMessagesAsync(
            attackerId,
            conversation.AiMentorConversationId,
            CancellationToken.None));
    }

    private static AiMentorService CreateService(
        RoadmapPlatform.Infrastructure.Data.ApplicationDbContext context)
    {
        return new AiMentorService(
            context,
            new RecordingAiCreditService(),
            Options.Create(new AiSettings
            {
                ApiKey = "tester4-placeholder-key",
                GenerationModel = "gemini-2.5-flash"
            }));
    }

    private static AiMentorConversation CreateConversation(
        Guid userId,
        string title,
        DateTime updatedAt,
        DateTime? archivedAt)
    {
        return new AiMentorConversation
        {
            AiMentorConversationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            PageContext = "roadmap_selection",
            ArchivedAt = archivedAt,
            CreatedAt = updatedAt.AddMinutes(-5),
            UpdatedAt = updatedAt
        };
    }
}

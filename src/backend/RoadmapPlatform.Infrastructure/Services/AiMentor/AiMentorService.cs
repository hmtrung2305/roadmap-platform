using System.Text;
using System.Text.Json;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.AiMentor;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.AiMentor;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;

namespace RoadmapPlatform.Infrastructure.Services.AiMentor;

public sealed class AiMentorService : IAiMentorService
{
    private const string UserRole = "user";
    private const string AssistantRole = "assistant";
    private const string AiMentorFeatureName = "ai_mentor_chat";
    private const int AiMentorCreditCost = 1;
    private const int RecentMessageLimit = 10;
    private const int RoadmapsPerCareerRole = 3;
    private const int RepositoryInsightLimit = 5;
    private const int SkillGapHistoryLimit = 3;

    private readonly ApplicationDbContext _context;
    private readonly IAiCreditService _aiCreditService;
    private readonly AiSettings _aiSettings;
    private readonly Client _client;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public AiMentorService(
        ApplicationDbContext context,
        IAiCreditService aiCreditService,
        IOptions<AiSettings> aiOptions)
    {
        _context = context;
        _aiCreditService = aiCreditService;
        _aiSettings = aiOptions.Value;

        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key was not configured.");
        }

        _client = new Client(apiKey: _aiSettings.ApiKey);
    }

    public async Task<IReadOnlyList<AiMentorConversationDto>> GetConversationsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return await _context.AiMentorConversations
            .AsNoTracking()
            .Where(conversation =>
                conversation.UserId == id &&
                conversation.ArchivedAt == null)
            .OrderByDescending(conversation => conversation.UpdatedAt)
            .Select(conversation => new AiMentorConversationDto
            {
                AiMentorConversationId = conversation.AiMentorConversationId,
                Title = conversation.Title,
                PageContext = conversation.PageContext,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AiMentorMessageDto>> GetMessagesAsync(
        Guid id,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var ownsConversation = await _context.AiMentorConversations
            .AsNoTracking()
            .AnyAsync(conversation =>
                conversation.AiMentorConversationId == conversationId &&
                conversation.UserId == id &&
                conversation.ArchivedAt == null,
                cancellationToken);

        if (!ownsConversation)
        {
            throw new NotFoundException("AI mentor conversation was not found.");
        }

        var messages = await _context.AiMentorMessages
            .AsNoTracking()
            .Where(message => message.AiMentorConversationId == conversationId)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);

        return messages.Select(MapMessage).ToList();
    }

    public async Task ArchiveConversationAsync(
        Guid id,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _context.AiMentorConversations
            .FirstOrDefaultAsync(item =>
                item.AiMentorConversationId == conversationId &&
                item.UserId == id &&
                item.ArchivedAt == null,
                cancellationToken);

        if (conversation == null)
        {
            throw new NotFoundException("AI mentor conversation was not found.");
        }

        var now = DateTime.UtcNow;

        conversation.ArchivedAt = now;
        conversation.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<AiMentorChatResponseDto> AskAsync(
        Guid id,
        AiMentorChatRequestDto request,
        CancellationToken cancellationToken)
    {
        var content = request.Message?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ConflictException("Message is required.");
        }

        var conversation = await GetOrCreateConversationAsync(
            id,
            request.ConversationId,
            request.PageContext,
            content,
            cancellationToken);

        var recentMessages = await LoadRecentMessagesAsync(
            conversation.AiMentorConversationId,
            cancellationToken);

        var mentorContext = await BuildMentorContextAsync(
            id,
            cancellationToken);

        await _aiCreditService.SpendAsync(
            id,
            AiMentorFeatureName,
            AiMentorCreditCost,
            conversation.AiMentorConversationId,
            cancellationToken: cancellationToken);

        var assistantContent = await GenerateAssistantAnswerAsync(
            content,
            recentMessages,
            mentorContext.Text,
            cancellationToken);

        var now = DateTime.UtcNow;
        var modelName = GetGenerationModelName();

        var userMessage = new AiMentorMessage
        {
            AiMentorMessageId = Guid.NewGuid(),
            AiMentorConversationId = conversation.AiMentorConversationId,
            Role = UserRole,
            Content = content,
            Sources = "[]",
            AiModel = null,
            CreatedAt = now
        };

        var assistantMessage = new AiMentorMessage
        {
            AiMentorMessageId = Guid.NewGuid(),
            AiMentorConversationId = conversation.AiMentorConversationId,
            Role = AssistantRole,
            Content = assistantContent,
            Sources = JsonSerializer.Serialize(mentorContext.Sources, JsonOptions),
            AiModel = modelName,
            CreatedAt = now.AddMilliseconds(1)
        };

        conversation.UpdatedAt = now;

        _context.AiMentorMessages.Add(userMessage);
        _context.AiMentorMessages.Add(assistantMessage);

        await _context.SaveChangesAsync(cancellationToken);

        return new AiMentorChatResponseDto
        {
            Conversation = MapConversation(conversation),
            UserMessage = MapMessage(userMessage),
            AssistantMessage = MapMessage(assistantMessage)
        };
    }

    private async Task<AiMentorConversation> GetOrCreateConversationAsync(
        Guid userId,
        Guid? conversationId,
        string? pageContext,
        string firstMessage,
        CancellationToken cancellationToken)
    {
        if (conversationId.HasValue && conversationId.Value != Guid.Empty)
        {
            var existingConversation = await _context.AiMentorConversations
                .FirstOrDefaultAsync(conversation =>
                    conversation.AiMentorConversationId == conversationId.Value &&
                    conversation.UserId == userId &&
                    conversation.ArchivedAt == null,
                    cancellationToken);

            if (existingConversation == null)
            {
                throw new NotFoundException("AI mentor conversation was not found.");
            }

            return existingConversation;
        }

        var now = DateTime.UtcNow;

        var conversation = new AiMentorConversation
        {
            AiMentorConversationId = Guid.NewGuid(),
            UserId = userId,
            Title = BuildConversationTitle(firstMessage),
            PageContext = NormalizePageContext(pageContext),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.AiMentorConversations.Add(conversation);

        return conversation;
    }

    private async Task<IReadOnlyList<AiMentorMessage>> LoadRecentMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _context.AiMentorMessages
            .AsNoTracking()
            .Where(message => message.AiMentorConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAt)
            .Take(RecentMessageLimit)
            .OrderBy(message => message.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    private async Task<MentorContext> BuildMentorContextAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var sources = new List<AiMentorSourceDto>();
        var builder = new StringBuilder();

        await AppendUserProfileContextAsync(builder, sources, userId, cancellationToken);
        await AppendPublishedRoadmapContextAsync(builder, sources, cancellationToken);
        await AppendGitHubInsightContextAsync(builder, sources, userId, cancellationToken);
        await AppendSkillGapContextAsync(builder, sources, userId, cancellationToken);

        return new MentorContext(builder.ToString(), sources);
    }

    private async Task AppendUserProfileContextAsync(
        StringBuilder builder,
        List<AiMentorSourceDto> sources,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var profile = await _context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        builder.AppendLine("[USER PROFILE]");

        if (profile == null)
        {
            builder.AppendLine("No user profile is available.");
            return;
        }

        builder.AppendLine($"Display name: {TrimForPrompt(profile.DisplayName, 120)}");
        builder.AppendLine($"Headline: {TrimForPrompt(profile.Headline, 180)}");
        builder.AppendLine($"Current role: {TrimForPrompt(profile.CurrentRole, 120)}");
        builder.AppendLine($"Career goal: {TrimForPrompt(profile.CareerGoal, 180)}");
        builder.AppendLine($"Bio: {TrimForPrompt(profile.Bio, 600)}");
        builder.AppendLine($"GitHub URL: {TrimForPrompt(profile.GithubUrl, 180)}");
        builder.AppendLine($"Resume URL: {TrimForPrompt(profile.ResumeUrl, 180)}");

        sources.Add(new AiMentorSourceDto
        {
            Type = "user_profile",
            Title = "User profile",
            Detail = "Career goal, current role, headline, bio, GitHub URL, and resume URL."
        });
    }

    private async Task AppendPublishedRoadmapContextAsync(
        StringBuilder builder,
        List<AiMentorSourceDto> sources,
        CancellationToken cancellationToken)
    {
        var roadmapCandidates = await _context.RoadmapVersions
           .AsNoTracking()
           .Include(version => version.Roadmap)
               .ThenInclude(roadmap => roadmap.CareerRole)
           .Where(version =>
               version.Status == "published" &&
               version.Roadmap.Visibility == "public")
           .OrderBy(version => version.Roadmap.CareerRole.Name)
           .ThenByDescending(version => version.PublishedAt ?? version.UpdatedAt)
           .Select(version => new
           {
               RoadmapTitle = version.Roadmap.Title,
               RoadmapDescription = version.Roadmap.Description,
               CareerRole = version.Roadmap.CareerRole.Name,
               VersionTitle = version.Title,
               EstimatedHours = version.EstimatedTotalHours,
               PublishedAt = version.PublishedAt,
               UpdatedAt = version.UpdatedAt
           })
           .ToListAsync(cancellationToken);

        var roadmapsByCareerRole = roadmapCandidates
            .GroupBy(roadmap => roadmap.CareerRole)
            .OrderBy(group => group.Key)
            .Select(group => new
            {
                CareerRole = group.Key,
                Roadmaps = group
                    .OrderByDescending(roadmap => roadmap.PublishedAt ?? roadmap.UpdatedAt)
                    .Take(RoadmapsPerCareerRole)
                    .ToList()
            })
            .ToList();

        builder.AppendLine();
        builder.AppendLine("[PUBLISHED ROADMAP CATALOG BY CAREER ROLE]");

        if (roadmapsByCareerRole.Count == 0)
        {
            builder.AppendLine("No published roadmaps are available.");
        }
        else
        {
            foreach (var roleGroup in roadmapsByCareerRole)
            {
                builder.AppendLine($"Career role: {roleGroup.CareerRole}");

                foreach (var roadmap in roleGroup.Roadmaps)
                {
                    builder.AppendLine($"- {roadmap.RoadmapTitle}");
                    builder.AppendLine($"  Version: {roadmap.VersionTitle}");
                    builder.AppendLine($"  Estimated hours: {roadmap.EstimatedHours?.ToString() ?? "Unknown"}");
                    builder.AppendLine($"  Description: {TrimForPrompt(roadmap.RoadmapDescription, 250)}");
                }
            }

            sources.Add(new AiMentorSourceDto
            {
                Type = "roadmap_catalog",
                Title = "Published roadmap catalog by career role",
                Detail = $"{roadmapsByCareerRole.Count} career role(s), up to {RoadmapsPerCareerRole} roadmap(s) per role included."
            });
        }
    }

    private async Task AppendGitHubInsightContextAsync(
        StringBuilder builder,
        List<AiMentorSourceDto> sources,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var repositories = await _context.Repositories
            .AsNoTracking()
            .Include(repository => repository.RepoInsight)
            .Where(repository =>
                repository.UserId == userId &&
                repository.RepoInsight != null &&
                repository.RepoInsight.AnalysisStatus == "completed")
            .OrderByDescending(repository => repository.RepoInsight!.UpdatedAt)
            .Take(RepositoryInsightLimit)
            .Select(repository => new
            {
                repository.Name,
                repository.FullName,
                repository.HtmlUrl,
                repository.Description,
                repository.PrimaryLanguage,
                InsightSummary = repository.RepoInsight!.Summary,
                repository.RepoInsight.TechStack,
                repository.RepoInsight.DetectedSkills,
                repository.RepoInsight.ProjectType
            })
            .ToListAsync(cancellationToken);

        builder.AppendLine();
        builder.AppendLine("[GITHUB REPOSITORY INSIGHTS]");

        if (repositories.Count == 0)
        {
            builder.AppendLine("No analyzed GitHub repository insight is available.");
            return;
        }

        foreach (var repository in repositories)
        {
            builder.AppendLine($"- {repository.FullName}");
            builder.AppendLine($"  URL: {repository.HtmlUrl}");
            builder.AppendLine($"  Language: {TrimForPrompt(repository.PrimaryLanguage, 80)}");
            builder.AppendLine($"  Description: {TrimForPrompt(repository.Description, 250)}");
            builder.AppendLine($"  Project type: {TrimForPrompt(repository.ProjectType, 120)}");
            builder.AppendLine($"  Summary: {TrimForPrompt(repository.InsightSummary, 400)}");
            builder.AppendLine($"  Tech stack: {TrimForPrompt(repository.TechStack, 300)}");
            builder.AppendLine($"  Detected skills: {TrimForPrompt(repository.DetectedSkills, 300)}");
        }

        sources.Add(new AiMentorSourceDto
        {
            Type = "github_insight",
            Title = "GitHub repository insights",
            Detail = $"{repositories.Count} analyzed repository/repositories included."
        });
    }

    private async Task AppendSkillGapContextAsync(
        StringBuilder builder,
        List<AiMentorSourceDto> sources,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var skillGapHistories = await _context.SkillGapAnalysisHistories
            .AsNoTracking()
            .Where(history =>
                history.UserId == userId &&
                !history.IsDeleted)
            .OrderByDescending(history => history.CreatedAt)
            .Take(SkillGapHistoryLimit)
            .Select(history => new
            {
                CareerRoleName = history.CareerRoleNameSnapshot,
                RoadmapTitle = history.RoadmapTitleSnapshot,
                RoadmapVersionTitle = history.RoadmapVersionTitleSnapshot,
                history.MatchedSkills,
                history.TotalSkills,
                history.MissingSkills,
                history.CreatedAt
            })
            .ToListAsync(cancellationToken);

        builder.AppendLine();
        builder.AppendLine("[SKILL GAP HISTORY]");

        if (skillGapHistories.Count == 0)
        {
            builder.AppendLine("No skill gap analysis history is available.");
            return;
        }

        foreach (var history in skillGapHistories)
        {
            builder.AppendLine(
                $"- {history.CareerRoleName} / {history.RoadmapTitle}: {history.MatchedSkills}/{history.TotalSkills} matched, {history.MissingSkills} missing skills. Roadmap version: {history.RoadmapVersionTitle}. Created at {history.CreatedAt:u}");
        }

        sources.Add(new AiMentorSourceDto
        {
            Type = "skill_gap_history",
            Title = "Skill gap history",
            Detail = $"{skillGapHistories.Count} recent skill gap result(s) included."
        });
    }

    private async Task<string> GenerateAssistantAnswerAsync(
        string currentQuestion,
        IReadOnlyList<AiMentorMessage> recentMessages,
        string mentorContext,
        CancellationToken cancellationToken)
    {
        var systemInstruction =
            "You are an AI Virtual Mentor for a career roadmap platform.\n\n" +
            "Use the provided platform context to help the learner choose roadmaps, prioritize skills, plan projects, and prepare for career growth.\n" +
            "Use chat history only to understand the ongoing conversation.\n" +
            "Do not invent GitHub analysis, transcript data, roadmap status, roadmap node order, exact first nodes, resources, quizzes, checkpoints, project results, achievements, or learning progress.\n" +
            "The roadmap context may only contain catalog-level summaries. If the context does not include a [ROADMAP STRUCTURE] section, you must not claim an exact official roadmap order, exact first node, exact resource list, quiz list, or checkpoint list.\n" +
            "When asked for exact roadmap structure but only roadmap catalog summary is available, clearly say that the exact structure is not available in the current context. You may give a general recommendation, but label it as general guidance rather than the official roadmap order.\n" +
            "For requests to build a complete application or generate an entire project/source code, do not produce the whole project. Reframe the request into a learning plan, MVP breakdown, architecture outline, or next small implementation step.\n" +
            "If useful context is missing, clearly say what is missing and suggest how the learner can provide it.\n" +
            "Keep the answer practical, structured, and concise.\n" +
            "Avoid mentioning internal database table names, IDs, or implementation details.";

        var prompt = BuildPrompt(
            currentQuestion,
            recentMessages,
            mentorContext);

        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Role = "system",
                Parts = new List<Part>
                {
                    new()
                    {
                        Text = systemInstruction
                    }
                }
            }
        };

        var response = await _client.Models.GenerateContentAsync(
            model: GetGenerationModelName(),
            contents: prompt,
            config: config);

        return response?.Candidates?[0]?.Content?.Parts?[0]?.Text
            ?? "No answer was generated.";
    }

    private static string BuildPrompt(
        string currentQuestion,
        IReadOnlyList<AiMentorMessage> recentMessages,
        string mentorContext)
    {
        var builder = new StringBuilder();

        builder.AppendLine("[CONTEXT SCOPE]");
        builder.AppendLine("- The platform context may include user profile, roadmap catalog summaries, GitHub repository insights, skill gap history, and recent chat history.");
        builder.AppendLine("- The current roadmap catalog context is summary-level only.");
        builder.AppendLine("- Unless a [ROADMAP STRUCTURE] section is explicitly present, the context does not include roadmap nodes, node order, resources, quizzes, or checkpoints.");
        builder.AppendLine("- Do not claim exact roadmap structure from catalog summaries alone.");

        builder.AppendLine();
        builder.AppendLine("[PLATFORM CONTEXT]");
        builder.AppendLine(mentorContext);

        if (recentMessages.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("[RECENT CHAT HISTORY]");

            foreach (var message in recentMessages)
            {
                if (!IsAllowedRole(message.Role) || string.IsNullOrWhiteSpace(message.Content))
                {
                    continue;
                }

                builder.AppendLine($"{NormalizeRole(message.Role)}: {TrimForPrompt(message.Content, 1000)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("[CURRENT USER QUESTION]");
        builder.AppendLine(currentQuestion.Trim());

        return builder.ToString();
    }

    private string GetGenerationModelName()
    {
        return string.IsNullOrWhiteSpace(_aiSettings.GenerationModel)
            ? "gemini-2.5-flash"
            : _aiSettings.GenerationModel;
    }

    private static string BuildConversationTitle(string firstMessage)
    {
        var normalized = firstMessage.Trim();

        return normalized.Length <= 60 ? normalized : normalized[..57] + "...";
    }

    private static string NormalizePageContext(string? pageContext)
    {
        if (string.IsNullOrWhiteSpace(pageContext))
        {
            return "roadmap_selection";
        }

        var normalized = pageContext.Trim().ToLowerInvariant();

        return normalized.Length <= 100 ? normalized : normalized[..100];
    }

    private static AiMentorConversationDto MapConversation(AiMentorConversation conversation)
    {
        return new AiMentorConversationDto
        {
            AiMentorConversationId = conversation.AiMentorConversationId,
            Title = conversation.Title,
            PageContext = conversation.PageContext,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt
        };
    }

    private static AiMentorMessageDto MapMessage(AiMentorMessage message)
    {
        return new AiMentorMessageDto
        {
            AiMentorMessageId = message.AiMentorMessageId,
            AiMentorConversationId = message.AiMentorConversationId,
            Role = message.Role,
            Content = message.Content,
            Sources = DeserializeSources(message.Sources),
            AiModel = message.AiModel,
            CreatedAt = message.CreatedAt
        };
    }

    private static IReadOnlyList<AiMentorSourceDto> DeserializeSources(string? rawSource)
    {
        if (string.IsNullOrWhiteSpace(rawSource))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<AiMentorSourceDto>>(rawSource, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool IsAllowedRole(string? role)
    {
        var normalized = NormalizeRole(role);

        return normalized == UserRole || normalized == AssistantRole;
    }

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role)
            ? UserRole
            : role.Trim().ToLowerInvariant();
    }

    private static string TrimForPrompt(string? value, int maxCharacters)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Not provided";
        }

        var normalized = value.Trim();

        return normalized.Length <= maxCharacters
            ? normalized
            : normalized[..maxCharacters] + "...";
    }

    private sealed record MentorContext(
        string Text,
        IReadOnlyList<AiMentorSourceDto> Sources);
}

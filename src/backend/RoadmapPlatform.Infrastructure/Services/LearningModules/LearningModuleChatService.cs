using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Application.DTOs.LearningModules;
using RoadmapPlatform.Application.Exceptions;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.LearningModules;
using RoadmapPlatform.Infrastructure.Configurations;
using RoadmapPlatform.Infrastructure.Data;
using RoadmapPlatform.Infrastructure.Entities;
using System.Text;

namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleChatService : ILearningModuleChatService
{
    private const string LearningModuleChatFeatureName = "learning_module_chat";
    private const int LearningModuleChatCreditCost = 1;

    private readonly ApplicationDbContext _context;
    private readonly ILearningModuleRagIndexingService _ragIndexingService;
    private readonly IAiCreditService _aiCreditService;
    private readonly AiSettings _aiSettings;
    private readonly LearningModuleRagSettings _ragSettings;
    private readonly Client _client;

    public LearningModuleChatService(
        ApplicationDbContext context,
        ILearningModuleRagIndexingService ragIndexingService,
        IAiCreditService aiCreditService,
        IOptions<AiSettings> aiOptions,
        IOptions<LearningModuleRagSettings> ragOptions)
    {
        _context = context;
        _ragIndexingService = ragIndexingService;
        _aiCreditService = aiCreditService;
        _aiSettings = aiOptions.Value;
        _ragSettings = ragOptions.Value;

        if (string.IsNullOrWhiteSpace(_aiSettings.ApiKey))
        {
            throw new InvalidOperationException("Gemini API key was not configured.");
        }

        _client = new Client(apiKey: _aiSettings.ApiKey);
    }

    public async Task<LearningModuleChatResponseDto> AskAsync(
        Guid userId,
        Guid skillModuleId,
        LearningModuleChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ConflictException("Message is required.");
        }

        var module = await _context.SkillModules
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.SkillModuleId == skillModuleId
                && (
                    item.Status == LearningModuleStatusValues.Published
                    || item.Status == LearningModuleStatusValues.Archived),
                cancellationToken);

        if (module == null)
        {
            throw new NotFoundException("Learning module was not found.");
        }

        var hasEnrollment = await _context.SkillModuleEnrollments
            .AsNoTracking()
            .AnyAsync(item =>
                item.UserId == userId
                && item.SkillModuleId == skillModuleId,
                cancellationToken);

        if (!hasEnrollment)
        {
            throw new ConflictException("Start the module before using module chat.");
        }

        if (request.SkillModuleLessonId.HasValue)
        {
            var lessonExists = await _context.SkillModuleLessons
                .AsNoTracking()
                .AnyAsync(item =>
                    item.SkillModuleLessonId == request.SkillModuleLessonId.Value
                    && item.SkillModuleId == skillModuleId,
                    cancellationToken);

            if (!lessonExists)
            {
                throw new NotFoundException("Learning module lesson was not found.");
            }
        }

        var retrievalQuery = BuildRetrievalQuery(request);

        var ragSources = await _ragIndexingService.SearchRelevantChunksAsync(
            skillModuleId,
            request.SkillModuleLessonId,
            retrievalQuery,
            _ragSettings.MaxChunks,
            cancellationToken);

        if (ragSources.Count == 0)
        {
            return new LearningModuleChatResponseDto
            {
                Answer = "I could not find anything in this module's lesson content that answers that question.",
                Sources = []
            };
        }

        var sourceIds = ragSources
            .Select(source => source.SkillModuleChunkId)
            .ToList();

        var chunks = await _context.SkillModuleChunks
            .AsNoTracking()
            .Include(chunk => chunk.SkillModuleLesson)
            .Where(chunk => sourceIds.Contains(chunk.SkillModuleChunkId))
            .ToListAsync(cancellationToken);

        var chunksById = chunks.ToDictionary(
            chunk => chunk.SkillModuleChunkId,
            chunk => chunk);

        var orderedChunks = ragSources
            .Where(source => chunksById.ContainsKey(source.SkillModuleChunkId))
            .Select(source => chunksById[source.SkillModuleChunkId])
            .ToList();

        if (orderedChunks.Count == 0)
        {
            return new LearningModuleChatResponseDto
            {
                Answer = "I could not find readable lesson content for that question.",
                Sources = []
            };
        }

        await _aiCreditService.SpendAsync(
            userId,
            LearningModuleChatFeatureName,
            LearningModuleChatCreditCost,
            skillModuleId,
            cancellationToken: cancellationToken);

        var answer = await GenerateAnswerAsync(
            request,
            orderedChunks,
            cancellationToken);

        return new LearningModuleChatResponseDto
        {
            Answer = answer,
            Sources = ragSources
                .Select(source => new LearningModuleChatSourceDto
                {
                    SkillModuleChunkId = source.SkillModuleChunkId,
                    SkillModuleLessonId = source.SkillModuleLessonId,
                    LessonTitle = source.LessonTitle,
                    Heading = source.Heading,
                    ContentPreview = source.ContentPreview,
                    SimilarityScore = source.SimilarityScore
                })
                .ToList()
        };
    }

    private async Task<string> GenerateAnswerAsync(
        LearningModuleChatRequestDto request,
        IReadOnlyList<SkillModuleChunk> chunks,
        CancellationToken cancellationToken)
    {
        var systemInstruction = "You are a learning module chat assistant.\n\n" +
                        "Use the provided module lesson context as the primary source of truth.\n" +
                        "You may explain concepts, connect related ideas, infer simple relationships, and provide small examples when they are consistent with the module context.\n\n" +
                        "Do not invent unsupported project-specific facts, APIs, requirements, file names, code behavior, or claims that are not grounded in the module context.\n\n" +
                        "If the module context does not contain enough information to answer the question, say that the module does not fully cover it instead of guessing.\n\n" +
                        "Keep the answer clear, practical, and useful for a learner.\n" +
                        "Do not mention internal chunk IDs.";

        var prompt = BuildPrompt(request, chunks);

        var config = new GenerateContentConfig
        {
            SystemInstruction = new Content
            {
                Role = "system",
                Parts = new List<Part>
                {
                    new Part
                    {
                        Text = systemInstruction
                    }
                }
            }
        };

        var response = await _client.Models.GenerateContentAsync(
            model: string.IsNullOrWhiteSpace(_aiSettings.GenerationModel)
                ? "gemini-2.5-flash"
                : _aiSettings.GenerationModel,
            contents: prompt,
            config: config);

        return response?.Candidates?[0]?.Content?.Parts?[0]?.Text
            ?? "No answer was generated.";
    }

    private static string BuildPrompt(
        LearningModuleChatRequestDto request,
        IReadOnlyList<SkillModuleChunk> chunks)
    {
        var builder = new StringBuilder();

        builder.AppendLine("[MODULE LESSON CONTEXT]");

        foreach (var chunk in chunks)
        {
            builder.AppendLine();
            builder.AppendLine("---");
            builder.AppendLine($"Lesson: {chunk.SkillModuleLesson.Title}");

            if (!string.IsNullOrWhiteSpace(chunk.Heading))
            {
                builder.AppendLine($"Section: {chunk.Heading}");
            }

            builder.AppendLine();
            builder.AppendLine(chunk.Content);
        }

        var recentMessages = request.RecentMessages
            .Where(message =>
                !string.IsNullOrWhiteSpace(message.Content)
                && IsAllowedRole(message.Role))
            .TakeLast(6)
            .ToList();

        if (recentMessages.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("[RECENT CHAT CONTEXT]");

            foreach (var message in recentMessages)
            {
                builder.AppendLine($"{NormalizeRole(message.Role)}: {message.Content.Trim()}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("[USER QUESTION]");
        builder.AppendLine(request.Message.Trim());

        return builder.ToString();
    }
    private static string BuildRetrievalQuery(LearningModuleChatRequestDto request)
    {
        var builder = new StringBuilder();

        var recentMessages = request.RecentMessages
            .Where(message =>
                !string.IsNullOrWhiteSpace(message.Content)
                && IsAllowedRole(message.Role))
            .TakeLast(4)
            .ToList();

        if (recentMessages.Count > 0)
        {
            builder.AppendLine("Recent conversation:");

            foreach (var message in recentMessages)
            {
                var role = NormalizeRole(message.Role);
                var content = TrimForRetrieval(message.Content);

                builder.AppendLine($"{role}: {content}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("Current question:");
        builder.AppendLine(request.Message.Trim());

        return builder.ToString().Trim();
    }

    private static string TrimForRetrieval(string content)
    {
        const int maxCharacters = 600;

        var normalized = content.Trim();

        if (normalized.Length <= maxCharacters)
        {
            return normalized;
        }

        return normalized[..maxCharacters];
    }

    private static bool IsAllowedRole(string? role)
    {
        var normalized = NormalizeRole(role);

        return normalized is "user" or "assistant";
    }

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role)
            ? "user"
            : role.Trim().ToLowerInvariant();
    }
}

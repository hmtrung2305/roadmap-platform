using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Chat;
using RoadmapPlatform.Application.DTOs.Rag;
using RoadmapPlatform.Application.Interfaces.AiCredits;
using RoadmapPlatform.Application.Interfaces.Chat;
using RoadmapPlatform.Application.Interfaces.Rag;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Services.Chat
{
    public class ChatService : IChatService
    {
        private const string ChatFeatureName = "ai_chat";
        private const int ChatCreditCost = 1;

        private readonly IAiCreditService _aiCreditService;
        private readonly ApplicationDbContext _context;
        private readonly IRagService _ragService;

        public ChatService(
            ApplicationDbContext context,
            IAiCreditService aiCreditService,
            IRagService ragService)
        {
            _context = context;
            _aiCreditService = aiCreditService;
            _ragService = ragService;
        }

        public async Task<ChatResponseDto> ChatAsync(Guid userId, ChatRequestDto request)
        {
            if (request.ResourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                throw new ArgumentException("Prompt is required.");
            }

            var resourceExists = await _context.Resources
                .AsNoTracking()
                .AnyAsync(resource => resource.ResourceId == request.ResourceId);

            if (!resourceExists)
            {
                throw new KeyNotFoundException("Resource was not found.");
            }

            await _aiCreditService.EnsureCanSpendAsync(userId, ChatFeatureName, ChatCreditCost);

            if (_ragService.IsKnowledgeBaseEmpty())
            {
                var chunks = await _context.ResourceChunks
                    .AsNoTracking()
                    .Where(chunk => chunk.Embedding != null)
                    .Select(chunk => new RagKnowledgeChunkDto
                    {
                        ChunkId = chunk.ChunkId,
                        ResourceId = chunk.ResourceId,
                        Content = chunk.ChunkContent,
                        Vector = chunk.Embedding!.ToArray()
                    })
                    .ToListAsync();

                _ragService.LoadKnowledgeBase(chunks);
            }

            var result = await _ragService.GenerateAnswerAsync(
                request.Prompt.Trim(),
                request.ResourceId
            );

            var credits = await _aiCreditService.RecordUsageAsync(
                userId,
                ChatFeatureName,
                ChatCreditCost);

            return new ChatResponseDto
            {
                Response = result.Answer,
                DebugContextUsed = result.Context,
                DebugScore = result.Score,
                Credits = credits
            };
        }
    }
}
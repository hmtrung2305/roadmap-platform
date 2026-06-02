using Microsoft.EntityFrameworkCore;
using RoadmapPlatform.Application.DTOs.Chat;
using RoadmapPlatform.Application.DTOs.Rag;
using RoadmapPlatform.Application.Interfaces.Chat;
using RoadmapPlatform.Application.Interfaces.Rag;
using RoadmapPlatform.Infrastructure.Data;

namespace RoadmapPlatform.Infrastructure.Services.Chat
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRagService _ragService;

        public ChatService(ApplicationDbContext context, IRagService ragService)
        {
            _context = context;
            _ragService = ragService;
        }

        public async Task<ChatResponseDto> ChatAsync(ChatRequestDto request)
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

            return new ChatResponseDto
            {
                Response = result.Answer,
                DebugContextUsed = result.Context,
                DebugScore = result.Score
            };
        }
    }
}
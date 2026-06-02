using RoadmapPlatform.Application.DTOs.Rag;

namespace RoadmapPlatform.Application.Interfaces.Rag
{
    public interface IRagService
    {
        Task<float[]> CreateEmbeddingAsync(string text);

        void LoadKnowledgeBase(IEnumerable<RagKnowledgeChunkDto> chunks);

        Task<RagResultDto> GenerateAnswerAsync(string prompt, Guid resourceId);

        bool IsKnowledgeBaseEmpty();
    }
}
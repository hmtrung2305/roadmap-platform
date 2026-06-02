namespace RoadmapPlatform.Application.DTOs.Rag
{
    public class RagKnowledgeChunkDto
    {
        public Guid ChunkId { get; set; }

        public Guid ResourceId { get; set; }

        public string Content { get; set; } = string.Empty;

        public float[] Vector { get; set; } = Array.Empty<float>();
    }
}
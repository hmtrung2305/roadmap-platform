namespace RoadmapPlatform.Infrastructure.Configurations
{
    public class RagSettings
    {
        public float SimilarityThreshold { get; set; } = 0.6f;

        public int MaxChunks { get; set; } = 3;
    }
}
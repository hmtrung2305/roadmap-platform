namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class LearningModuleRagSettings
{
    public int EmbeddingDimensions { get; set; } = 3072;

    public float SimilarityThreshold { get; set; } = 0.55f;

    public int MaxChunks { get; set; } = 5;

    public int TargetChunkCharacters { get; set; } = 1800;

    public int MaxChunkCharacters { get; set; } = 2600;
}

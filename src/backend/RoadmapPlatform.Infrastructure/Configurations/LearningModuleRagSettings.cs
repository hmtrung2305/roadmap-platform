namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class LearningModuleRagSettings
{
    public int EmbeddingDimensions { get; set; } = 3072;

    public float SimilarityThreshold { get; set; } = 0.6f;

    public int MaxChunks { get; set; } = 5;

    public int TargetChunkCharacters { get; set; } = 1800;

    public int MaxChunkCharacters { get; set; } = 2600;

    public int OverlapCharacters { get; set; } = 250;

    public int MaxOverlapBlocks { get; set; } = 1;
}

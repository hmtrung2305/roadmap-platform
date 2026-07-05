namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class LearningModuleIndexingSettings
{
    public const string SectionName = "LearningModuleIndexing";

    public bool Enabled { get; set; } = true;

    public int PollIntervalSeconds { get; set; } = 60;

    public int BatchSize { get; set; } = 5;

    public int StaleIndexingMinutes { get; set; } = 15;
}

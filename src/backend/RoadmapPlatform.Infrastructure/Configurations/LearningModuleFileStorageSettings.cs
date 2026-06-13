namespace RoadmapPlatform.Infrastructure.Configurations;

public sealed class LearningModuleFileStorageSettings
{
    public string Provider { get; set; } = "Local";

    public string LocalFolder { get; set; } = "learning-modules";
}

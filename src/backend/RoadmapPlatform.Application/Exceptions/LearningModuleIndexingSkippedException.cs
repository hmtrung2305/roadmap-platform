namespace RoadmapPlatform.Infrastructure.Services.LearningModules;

public sealed class LearningModuleIndexingSkippedException : Exception
{
    public LearningModuleIndexingSkippedException(string message)
        : base(message)
    {
    }
}

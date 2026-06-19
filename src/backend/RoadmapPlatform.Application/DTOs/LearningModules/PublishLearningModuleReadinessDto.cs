namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class PublishLearningModuleReadinessDto
{
    public bool CanPublish { get; set; }
    public IReadOnlyList<PublishLearningModuleReadinessCheckDto> Checks { get; set; } = [];
    public IReadOnlyList<string> Errors { get; set; } = [];
}

public sealed class PublishLearningModuleReadinessCheckDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
}

public sealed class PublishLearningModuleResultDto
{
    public Guid SkillModuleId { get; set; }
    public string Status { get; set; } = LearningModuleStatusValues.Published;
    public DateTimeOffset PublishedAt { get; set; }
    public PublishLearningModuleReadinessDto Readiness { get; set; } = new();
}

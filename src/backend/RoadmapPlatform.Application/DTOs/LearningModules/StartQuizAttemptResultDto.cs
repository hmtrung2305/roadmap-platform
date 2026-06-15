namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class StartQuizAttemptResultDto
{
    public Guid SkillModuleQuizAttemptId { get; set; }
    public Guid SkillModuleQuizId { get; set; }
    public int AttemptNo { get; set; }
    public string Status { get; set; } = LearningModuleQuizAttemptStatusValues.InProgress;
    public DateTimeOffset StartedAt { get; set; }
    public LearningModuleQuizDto Quiz { get; set; } = new();
}

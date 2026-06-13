namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleEnrollmentDto
{
    public Guid SkillModuleEnrollmentId { get; set; }
    public Guid UserId { get; set; }
    public Guid SkillModuleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public Guid? LastAccessedLessonId { get; set; }
    public decimal ProgressPercent { get; set; }
    public Dictionary<Guid, string> LessonProgress { get; set; } = [];
}

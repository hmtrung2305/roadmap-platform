namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class UpdateLessonProgressResultDto
{
    public Guid SkillModuleEnrollmentId { get; set; }
    public Guid SkillModuleLessonId { get; set; }
    public string LessonStatus { get; set; } = string.Empty;
    public decimal ProgressPercent { get; set; }
    public string EnrollmentStatus { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

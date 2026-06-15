namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class QuizAttemptSummaryDto
{
    public Guid SkillModuleQuizAttemptId { get; set; }
    public Guid SkillModuleQuizId { get; set; }
    public int AttemptNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public decimal? ScorePercent { get; set; }
    public int? EarnedPoints { get; set; }
    public int? TotalPoints { get; set; }
    public bool? Passed { get; set; }
}

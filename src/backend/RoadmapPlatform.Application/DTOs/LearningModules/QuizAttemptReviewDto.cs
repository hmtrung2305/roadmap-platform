namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class QuizAttemptReviewDto
{
    public Guid SkillModuleQuizAttemptId { get; set; }
    public Guid SkillModuleQuizId { get; set; }
    public Guid SkillModuleEnrollmentId { get; set; }
    public Guid UserId { get; set; }
    public int AttemptNo { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public decimal? ScorePercent { get; set; }
    public int? EarnedPoints { get; set; }
    public int? TotalPoints { get; set; }
    public bool? Passed { get; set; }
    public IReadOnlyList<QuizAnswerReviewDto> Answers { get; set; } = [];
}

public sealed class QuizAnswerReviewDto
{
    public Guid SkillModuleQuizAnswerId { get; set; }
    public Guid SkillModuleQuizQuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionExplanation { get; set; }
    public Guid SelectedOptionId { get; set; }
    public string SelectedOptionText { get; set; } = string.Empty;
    public Guid? CorrectOptionId { get; set; }
    public string? CorrectOptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int EarnedPoints { get; set; }
    public int QuestionPoints { get; set; }
}

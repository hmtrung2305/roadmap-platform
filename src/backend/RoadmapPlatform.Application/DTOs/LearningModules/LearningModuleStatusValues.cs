namespace RoadmapPlatform.Application.DTOs.LearningModules;

public static class LearningModuleStatusValues
{
    public const string Draft = "draft";
    public const string Published = "published";
    public const string Archived = "archived";
}

public static class LearningModuleEnrollmentStatusValues
{
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
}

public static class LearningModuleLessonProgressStatusValues
{
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
}

public static class LearningModuleQuizAttemptStatusValues
{
    public const string InProgress = "in_progress";
    public const string Submitted = "submitted";
    public const string Abandoned = "abandoned";
}

public static class LearningModuleQuestionTypeValues
{
    public const string SingleChoice = "single_choice";
}

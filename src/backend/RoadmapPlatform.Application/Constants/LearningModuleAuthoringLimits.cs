namespace RoadmapPlatform.Application.Constants;

public static class LearningModuleAuthoringLimits
{
    public const int ModuleTitleMaxLength = 200;
    public const int ModuleSlugMaxLength = 200;
    public const int ModuleDescriptionMaxLength = 2_000;
    public const int DifficultyLevelMaxLength = 30;

    public const int LessonTitleMaxLength = 200;
    public const int LessonSlugMaxLength = 200;
    public const int LessonSummaryMaxLength = 1_000;
    public const int LessonFileNameMaxLength = 255;
    public const int BulkUploadMaxLessonCount = 25;

    public const long MarkdownFileMaxBytes = 2L * 1024L * 1024L;
    public const long BulkMarkdownUploadMaxBytes = 10L * 1024L * 1024L;
    public const long ReplaceLessonUploadRequestMaxBytes = 3L * 1024L * 1024L;
    public const long BulkLessonUploadRequestMaxBytes = 12L * 1024L * 1024L;

    public const decimal EstimatedHoursMinValue = 0m;
    public const decimal EstimatedHoursMaxValue = 999.99m;
}

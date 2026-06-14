namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class BulkUploadLessonsResultDto
{
    public IReadOnlyList<BulkUploadedLessonDto> Lessons { get; set; } = [];
    public IReadOnlyList<BulkUploadLessonFailureDto> FailedLessons { get; set; } = [];
    public int UploadedCount => Lessons.Count;
    public int FailedCount => FailedLessons.Count;
    public bool HasFailures => FailedLessons.Count > 0;
}

public sealed class BulkUploadedLessonDto
{
    public string ClientId { get; set; } = string.Empty;
    public Guid SkillModuleLessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public string MarkdownFileName { get; set; } = string.Empty;
    public string MarkdownFileKey { get; set; } = string.Empty;
    public string? ContentHash { get; set; }
    public long ContentSizeBytes { get; set; }
    public int ContentVersion { get; set; }
    public string IndexingStatus { get; set; } = LearningModuleLessonIndexingStatusValues.Pending;
    public DateTimeOffset? IndexedAt { get; set; }
    public string? IndexingError { get; set; }
    public int ChunksGenerated { get; set; }
}

public sealed class BulkUploadLessonFailureDto
{
    public string ClientId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

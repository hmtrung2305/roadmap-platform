namespace RoadmapPlatform.Application.DTOs.ContentWorkspace;

public sealed class ContentWorkspaceOverviewDto
{
    public ContentWorkspaceMetricsDto Metrics { get; set; } = new();

    public IReadOnlyList<ContentWorkspaceModuleItemDto> ReadyToPublish { get; set; } = [];

    public IReadOnlyList<ContentWorkspaceAttentionItemDto> NeedsAttention { get; set; } = [];

    public IReadOnlyList<ContentWorkspaceModuleItemDto> RecentDrafts { get; set; } = [];

    public IReadOnlyList<ContentWorkspaceModuleItemDto> RecentlyPublished { get; set; } = [];
}

public sealed class ContentWorkspaceMetricsDto
{
    public int Drafts { get; set; }

    public int ReadyToPublish { get; set; }

    public int NeedsAttention { get; set; }

    public int Published { get; set; }
}

public sealed class ContentWorkspaceModuleItemDto
{
    public Guid SkillModuleId { get; set; }

    public Guid SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string SkillSlug { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? DifficultyLevel { get; set; }

    public int LessonCount { get; set; }

    public int QuestionCount { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class ContentWorkspaceAttentionItemDto
{
    public ContentWorkspaceModuleItemDto Module { get; set; } = new();

    public string CheckKey { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

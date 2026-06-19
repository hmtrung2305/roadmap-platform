namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ContentManagerLearningModuleListResultDto
{
    public IReadOnlyList<ContentManagerLearningModuleSummaryDto> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public ContentManagerLearningModuleStatusCountsDto StatusCounts { get; set; } = new();
}

public sealed class ContentManagerLearningModuleStatusCountsDto
{
    public int Draft { get; set; }

    public int Published { get; set; }

    public int Archived { get; set; }
}

namespace RoadmapPlatform.Application.DTOs.LearningResources;

public sealed class ContentLearningResourceSearchQueryDto
{
    public string? Search { get; set; }
    public string? ResourceType { get; set; }
    public string? DifficultyLevel { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public sealed class ContentLearningResourceSearchResultDto
{
    public IReadOnlyList<ContentLearningResourceDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public bool HasMore => Offset + Items.Count < TotalCount;
}

public sealed class ContentLearningResourceDto
{
    public Guid ResourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? DifficultyLevel { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public int NodeMappingCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class CreateContentLearningResourceRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? DifficultyLevel { get; set; }
}

public sealed class UpdateContentLearningResourceRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? DifficultyLevel { get; set; }
}

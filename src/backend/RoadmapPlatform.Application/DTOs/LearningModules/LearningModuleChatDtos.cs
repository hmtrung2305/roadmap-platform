using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleChatRequestDto
{
    public Guid? SkillModuleLessonId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public List<LearningModuleChatMessageDto> RecentMessages { get; set; } = [];
}

public sealed class LearningModuleChatMessageDto
{
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;
}

public sealed class LearningModuleChatResponseDto
{
    public string Answer { get; set; } = string.Empty;

    public IReadOnlyList<LearningModuleChatSourceDto> Sources { get; set; } = [];
}

public sealed class LearningModuleChatSourceDto
{
    public Guid SkillModuleChunkId { get; set; }

    public Guid SkillModuleLessonId { get; set; }

    public string LessonTitle { get; set; } = string.Empty;

    public string? Heading { get; set; }

    public string ContentPreview { get; set; } = string.Empty;

    public double? SimilarityScore { get; set; }
}

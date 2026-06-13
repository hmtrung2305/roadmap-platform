using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ModuleAssistantRequestDto
{
    public Guid? SkillModuleLessonId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public List<ModuleAssistantMessageDto> RecentMessages { get; set; } = [];
}

public sealed class ModuleAssistantMessageDto
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

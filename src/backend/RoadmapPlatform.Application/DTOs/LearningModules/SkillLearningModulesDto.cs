namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class SkillLearningModulesDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string SkillSlug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public IReadOnlyList<LearnerLearningModuleSummaryDto> Modules { get; set; } = [];
}

namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class ContentManagerLearningModuleDetailDto
{
    public SkillModuleDto Module { get; set; } = new();
    public IReadOnlyList<LearningModuleLessonDto> Lessons { get; set; } = [];
    public LearningModuleQuizDto? Quiz { get; set; }
    public PublishLearningModuleReadinessDto PublishReadiness { get; set; } = new();
}

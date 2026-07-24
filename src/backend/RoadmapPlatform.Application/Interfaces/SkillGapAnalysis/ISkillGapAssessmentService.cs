using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapAssessmentService
    {
        Task<AssessmentResponseDto> GetAssessmentAsync(
            Guid userId,
            Guid roadmapId,
            CancellationToken cancellationToken);
    }
}
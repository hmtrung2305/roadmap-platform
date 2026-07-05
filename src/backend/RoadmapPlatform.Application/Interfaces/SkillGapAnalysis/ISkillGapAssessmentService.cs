using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapAssessmentService
    {
        Task<AssessmentResponseDto> GetAssessmentAsync(Guid roadmapId);
    }
}

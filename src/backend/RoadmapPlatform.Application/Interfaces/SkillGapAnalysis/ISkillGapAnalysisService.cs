using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.CareerRoleSkill
{
    public interface ISkillGapAnalysisService
    {
        Task<AssessmentResponseDto> GetAssessmentAsync(string careerRoleSlug);

        Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(Guid userId, AnalyzeSkillGapRequestDto request);

        Task<List<CareerRoleOptionDto>> GetCareerRolesAsync();

        Task<List<AssessmentGroupAdminDto>> GetAssessmentGroupsAsync(string careerRoleSlug);

        Task UpdateAssessmentGroupsAsync(List<UpdateAssessmentGroupDto> request);

        Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId);

        Task<SkillGapHistoryDetailDto> GetHistoryDetailAsync(Guid historyId, Guid userId);

        Task DeleteHistoryAsync(Guid historyId, Guid userId);
    }
}

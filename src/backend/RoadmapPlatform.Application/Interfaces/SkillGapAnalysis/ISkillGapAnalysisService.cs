using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.CareerRoleSkill
{
    public interface ISkillGapAnalysisService
    {


        Task<List<CareerRoleOptionDto>> GetCareerRolesAsync();



        //USER
        Task<List<AssessmentLevelDto>> GetAssessmentLevelsAsync(string careerRoleSlug);

        Task<AssessmentByLevelResponseDto> GetAssessmentByLevelAsync(string careerRoleSlug, string levelSlug);

        Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(Guid userId, AnalyzeSkillGapRequestDto request);

        Task DeleteHistoryAsync(Guid historyId, Guid userId);

        Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId);

        Task<SkillGapHistoryDetailDto> GetHistoryDetailAsync(Guid historyId, Guid userId);




        // ContentManager
        Task UpdateAssessmentLevelGroupsAsync(string careerRoleSlug, string levelSlug, UpdateAssessmentLevelGroupsDto request);
        Task<List<AssessmentLevelContentManagerDto>> GetAssessmentLevelsContentManagerAsync(string careerRoleSlug);

        Task<AssessmentLevelGroupsContentManagerDto> GetAssessmentGroupsByLevelContentManagerAsync(string careerRoleSlug, string levelSlug);
    }
}

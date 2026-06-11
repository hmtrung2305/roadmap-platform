using RoadmapPlatform.Application.DTOs.Roadmaps;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.CareerRoleSkill
{
    public interface ISkillGapAnalysisService
    {
        Task<List<CareerRoleResponseDto>> GetAllCareerRolesAsync();
        Task<CareerRoleResponseDto> GetCareerRoleBySlugAsync(string slug);
        Task<AssessmentSkillResponseDto> GetAssessmentSkillBySlugAsync(string slug);
        Task<SkillGapResultResponseDto> GetSkillGapResultAsync(AnalyzeSkillGapRequestDto analyzeSkillGapRequest);
        Task<SkillGapReportResponseDto> GenerateSkillGapReportAsync(AnalyzeSkillGapRequestDto request);
    }
}

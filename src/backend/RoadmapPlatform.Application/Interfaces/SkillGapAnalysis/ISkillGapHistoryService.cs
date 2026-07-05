using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapHistoryService
    {
        Task<List<SkillGapHistoryDto>> GetHistoryAsync(Guid userId);

        Task<AnalyzeSkillGapResponseDto> GetHistoryDetailAsync(Guid userId, Guid skillGapAnalysisHistoryId);

        Task DeleteHistoryAsync(Guid userId, Guid skillGapAnalysisHistoryId);
    }
}

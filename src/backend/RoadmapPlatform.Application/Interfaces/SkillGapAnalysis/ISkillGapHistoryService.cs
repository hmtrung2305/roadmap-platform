using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.History;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapHistoryService
    {
        Task<SkillGapHistoryPageDto> GetHistoryAsync(Guid userId, SkillGapHistoryPageRequestDto request, CancellationToken cancellationToken);
        Task<AnalyzeSkillGapResponseDto> GetHistoryDetailAsync(Guid userId, Guid skillGapAnalysisHistoryId, CancellationToken cancellationToken);

        Task DeleteHistoryAsync(Guid userId, Guid skillGapAnalysisHistoryId, CancellationToken cancellationToken);
    }
}

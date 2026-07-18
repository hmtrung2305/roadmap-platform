using RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis;
using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Application.Interfaces.SkillGapAnalysis
{
    public interface ISkillGapAnalysisService
    {
        Task<AnalyzeSkillGapResponseDto> AnalyzeAsync(Guid userId, AnalyzeSkillGapRequestDto request, CancellationToken cancellationToken);
    }
}

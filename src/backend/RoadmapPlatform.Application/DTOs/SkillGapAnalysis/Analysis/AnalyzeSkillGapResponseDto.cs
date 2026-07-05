using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis
{
    public class AnalyzeSkillGapResponseDto
    {
        public Guid SkillGapAnalysisHistoryId { get; set; }

        public Guid RoadmapId { get; set; }

        public string RoadmapName { get; set; } = string.Empty;

        public string CareerRoleName { get; set; } = string.Empty;

        public string RoadmapVersionTitle { get; set; } = string.Empty;

        public int RoadmapVersionNumber { get; set; }

        public string AuthorName { get; set; } = string.Empty;

        public int MatchedSkills { get; set; }

        public int TotalSkills { get; set; }

        public int MissingSkills { get; set; }

        public List<AnalysisCategoryDto> Categories { get; set; } = [];
    }
}

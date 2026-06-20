using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public class AnalyzeSkillGapResponseDto
    {
        public string CareerRoleName { get; set; } = string.Empty;

        public int TotalSkills { get; set; }

        public int MatchedSkills { get; set; }

        public decimal SkillCoveragePercent { get; set; }

        public decimal ReadinessPercent { get; set; }

        public int TotalGroups { get; set; }

        public int CompletedGroups { get; set; }

        public int MissingGroups { get; set; }

        public List<SkillGapGroupResultDto> Groups { get; set; } = [];

    }
}

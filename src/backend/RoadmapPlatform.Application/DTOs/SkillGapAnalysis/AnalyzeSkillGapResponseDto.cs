using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public class AnalyzeSkillGapResponseDto
    {
        public string CareerRoleName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;

        public string LevelSlug { get; set; } = string.Empty;

        public int MissingSkills { get; set; }

        public int TotalSkills { get; set; }

        public int MatchedSkills { get; set; }

        public int TotalGroups { get; set; }

        public int CompletedGroups { get; set; }

        public int MissingGroups { get; set; }

        public List<SkillGapGroupResultDto> Groups { get; set; } = [];

    }
}

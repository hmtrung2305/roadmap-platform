using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class SkillGapGroupResultDto
    {
        public string GroupName { get; set; }
            = string.Empty;

        public string PhaseName { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public int TotalSkills { get; set; }

        public int MatchedSkills { get; set; }

        public string? SelectionType { get; set; } = string.Empty;

        public int? RequiredCount { get; set; }

        public bool IsCompleted { get; set; }

        public List<MissingSkillDto> MissingSkills { get; set; } = [];
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class SkillGapResultResponseDto
    {
        public string CareerRoleName { get; set; }
            = default!;

        public int TotalGroups { get; set; }

        public int CompletedGroups { get; set; }

        public int MissingGroups { get; set; }

        public decimal ReadinessPercent { get; set; }

        public List<GroupAnalysisDto> Groups { get; set; }
            = [];
        public List<MissingGroupDto> MissingGroupList
        { get; set; } = [];
    }
}

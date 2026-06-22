using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentLevelGroupsAdminDto
    {
        public string CareerRoleName { get; set; } = string.Empty;

        public string LevelName { get; set; }
            = string.Empty;

        public string LevelSlug { get; set; }
            = string.Empty;

        public int GroupCount { get; set; }

        public List<AssessmentGroupAdminDto> Groups { get; set; } = [];
    }
}

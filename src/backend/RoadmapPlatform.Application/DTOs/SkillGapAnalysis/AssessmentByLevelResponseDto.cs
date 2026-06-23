using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentByLevelResponseDto
    {
        public Guid LevelId { get; set; }

        public string LevelName { get; set; }
            = string.Empty;

        public string LevelSlug { get; set; }
            = string.Empty;

        public string CareerRoleName { get; set; }
            = string.Empty;

        public int RoadmapVersionNumber { get; set; }

        public string RoadmapVersionTitle { get; set; } = string.Empty;

        public List<AssessmentGroupByLevelDto>
            Groups
        { get; set; }
            = [];
    }
}

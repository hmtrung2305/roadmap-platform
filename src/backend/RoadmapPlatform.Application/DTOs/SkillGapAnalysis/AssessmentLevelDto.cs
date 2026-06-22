using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentLevelDto
    {
        public Guid LevelId { get; set; }

        public string LevelName { get; set; }
            = string.Empty;

        public string Slug { get; set; }
            = string.Empty;
    }
}

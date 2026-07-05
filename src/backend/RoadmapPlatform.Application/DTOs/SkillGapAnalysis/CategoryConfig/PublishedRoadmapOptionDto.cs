using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.CategoryConfig
{
    public class PublishedRoadmapOptionDto
    {
        public Guid RoadmapId { get; set; }

        public string RoadmapName { get; set; } = string.Empty;

        public string CareerRoleName { get; set; } = string.Empty;

        public string RoadmapVersionTitle { get; set; } = string.Empty;

        public int RoadmapVersionNumber { get; set; }
    }
}

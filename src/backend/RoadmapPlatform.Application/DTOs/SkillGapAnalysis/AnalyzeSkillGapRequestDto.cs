using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AnalyzeSkillGapRequestDto
    {
        public string CareerRoleSlug { get; set; }
            = string.Empty;
        public string LevelSlug { get; set; }
            = string.Empty;

        public List<Guid> SelectedNodeIds { get; set; }
            = [];
    }
}

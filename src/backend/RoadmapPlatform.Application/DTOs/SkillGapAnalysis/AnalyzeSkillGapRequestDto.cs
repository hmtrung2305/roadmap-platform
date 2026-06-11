using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AnalyzeSkillGapRequestDto
    {
        public string CareerRoleSlug { get; set; }
            = default!;

        public List<string> SelectedSkillSlugs { get; set; }
            = [];
    }
}

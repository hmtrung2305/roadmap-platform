using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis
{
    public class AnalyzeSkillGapRequestDto
    {
        public Guid RoadmapId { get; set; }

        public List<Guid> SelectedSkillIds { get; set; } = [];
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis
{
    public class AnalysisSkillDto
    {
        public Guid SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;

        public bool IsMatched { get; set; }
    }
}

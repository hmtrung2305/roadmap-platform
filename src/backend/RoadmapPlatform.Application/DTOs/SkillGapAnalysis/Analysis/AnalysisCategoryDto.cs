using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Analysis
{
    public class AnalysisCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public int MatchedSkills { get; set; }

        public int TotalSkills { get; set; }

        public int MissingSkills { get; set; }

        public List<AnalysisSkillDto> Skills { get; set; } = [];
    }
}

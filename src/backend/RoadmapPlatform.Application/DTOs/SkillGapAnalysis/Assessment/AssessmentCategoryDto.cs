using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment
{
    public class AssessmentCategoryDto
    {
        public string CategoryName { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public List<AssessmentSkillDto> Skills { get; set; }
            = new();
    }
}

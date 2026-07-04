using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis.Assessment
{
    public class AssessmentSkillDto
    {
        public Guid SkillId { get; set; }

        public string SkillName { get; set; } = string.Empty;
    }
}

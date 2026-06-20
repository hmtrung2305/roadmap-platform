using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class UpdateAssessmentGroupDto
    {
        public Guid GroupId { get; set; }

        public bool IsAssessmentSkill { get; set; }
    }
}

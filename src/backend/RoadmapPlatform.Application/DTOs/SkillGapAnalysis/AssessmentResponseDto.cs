using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentResponseDto
    {
        public string CareerRoleName { get; set; }
            = string.Empty;

        public List<AssessmentGroupDto> Groups { get; set; }
            = [];
    }
}

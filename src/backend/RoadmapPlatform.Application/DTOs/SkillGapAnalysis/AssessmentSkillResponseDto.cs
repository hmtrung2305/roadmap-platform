using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentSkillResponseDto
    {
        public Guid CareerRoleId { get; set; }

        public string CareerRoleName { get; set; }
            = default!;

        public List<AssessmentSkillGroupDto> SkillGroups
        { get; set; } = [];
    }
}

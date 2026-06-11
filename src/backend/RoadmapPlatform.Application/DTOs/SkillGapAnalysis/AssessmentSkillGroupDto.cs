using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentSkillGroupDto
    {
        public Guid SkillGroupId { get; set; }

        public string GroupName { get; set; } = default!;

        public int Priority { get; set; }

        public List<AssessmentSkillDto> Skills { get; set; }
            = [];
    }
}

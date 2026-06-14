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

        public string CompletionRule { get; set; }
            = default!;

        public int? RequiredSkillCount { get; set; }

        public string RequirementDescription { get; set; }
            = default!;

        public List<AssessmentSkillDto> Skills { get; set; }
            = [];
    }
}

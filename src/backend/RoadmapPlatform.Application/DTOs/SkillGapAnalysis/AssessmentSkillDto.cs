using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentSkillDto
    {
        public Guid SkillId { get; set; }

        public string Name { get; set; } = default!;

        public string Slug { get; set; } = default!;
    }
}

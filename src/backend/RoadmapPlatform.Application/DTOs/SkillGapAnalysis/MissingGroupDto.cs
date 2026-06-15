using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class MissingGroupDto
    {
        public Guid SkillGroupId { get; set; }

        public string GroupName { get; set; } = default!;

        public int Priority { get; set; }

        public LearningPriority LearningPriority { get; set; }
    }
}

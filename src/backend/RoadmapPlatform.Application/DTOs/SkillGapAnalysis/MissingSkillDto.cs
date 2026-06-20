using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class MissingSkillDto
    {
        public Guid NodeId { get; set; }

        public string Name { get; set; }
            = string.Empty;
    }
}

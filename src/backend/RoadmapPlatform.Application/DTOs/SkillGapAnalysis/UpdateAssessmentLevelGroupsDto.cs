using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class UpdateAssessmentLevelGroupsDto
    {
        public List<Guid> GroupIds { get; set; }
            = [];
    }
}

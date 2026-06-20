using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentNodeAdminDto
    {
        public Guid NodeId { get; set; }

        public string Name { get; set; }
            = string.Empty;

        public string NodeType { get; set; }
            = string.Empty;

        public bool IsAssessmentSkill { get; set; }
    }
}

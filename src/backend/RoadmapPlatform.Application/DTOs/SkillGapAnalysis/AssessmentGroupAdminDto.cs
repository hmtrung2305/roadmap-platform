using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentGroupAdminDto
    {
        public Guid GroupId { get; set; }

        public string GroupName { get; set; }
            = string.Empty;

        public string PhaseName { get; set; }
            = string.Empty;

        public int SortOrder { get; set; }

        public bool IsAssessmentSkill { get; set; }

        public int TotalTopics { get; set; }
    }
}

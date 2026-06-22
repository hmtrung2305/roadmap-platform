using System;
using System.Collections.Generic;
using System.Text;

namespace RoadmapPlatform.Application.DTOs.SkillGapAnalysis
{
    public sealed class AssessmentGroupByLevelDto
    {
        public Guid GroupId { get; set; }

        public string GroupName { get; set; }
            = string.Empty;

        public string GroupSlug { get; set; }
            = string.Empty;

        public string SelectionType { get; set; }
            = string.Empty;

        public string PhaseName { get; set; }
    = string.Empty;

        public int SortOrder { get; set; }

        public int? RequiredCount { get; set; }

        public List<AssessmentSkillDto>
            Skills
        { get; set; }
            = [];
    }
}
